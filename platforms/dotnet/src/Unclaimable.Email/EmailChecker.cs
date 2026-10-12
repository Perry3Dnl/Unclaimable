using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Unclaimable.Email;

/// <summary>
/// Validates practical unquoted mailbox addresses, applies Unclaimable to the local part,
/// and detects lookalikes of application-configured protected domains.
/// </summary>
public sealed class EmailChecker : IEmailChecker
{
    private sealed class ParsedAddress
    {
        public ParsedAddress(string localPart, string originalDomain, string domain)
        {
            LocalPart = localPart;
            OriginalDomain = originalDomain;
            Domain = domain;
        }

        public string LocalPart { get; }
        public string OriginalDomain { get; }
        public string Domain { get; }
    }

    private sealed class ProtectedDomain
    {
        public ProtectedDomain(string domain)
        {
            Domain = domain;
            RegistrantLabel = GetRegistrantLabel(domain);
            RegistrantSkeleton = CreateNormalizedDomainSkeleton(RegistrantLabel);
        }

        public string Domain { get; }
        public string RegistrantLabel { get; }
        public string RegistrantSkeleton { get; }
    }

    private sealed class DomainAssessment
    {
        public static DomainAssessment Safe { get; } =
            new DomainAssessment(DomainLookalikeKind.None, null);

        public DomainAssessment(DomainLookalikeKind kind, string? matchedProtectedDomain)
        {
            Kind = kind;
            MatchedProtectedDomain = matchedProtectedDomain;
        }

        public DomainLookalikeKind Kind { get; }
        public string? MatchedProtectedDomain { get; }
    }

    private readonly global::Unclaimable.IChecker _localPartChecker;
    private readonly ProtectedDomain[] _protectedDomains;
    private readonly string[] _issuingDomains;
    private readonly bool _allowIssuingDomainSubdomains;
    private readonly bool _enforceIssuingDomainsForExistingAddresses;
    private readonly bool _detectUnicodeLookalikes;
    private readonly bool _detectTypographicalLookalikes;
    private readonly bool _detectProtectedLabelReuse;
    private readonly int _maximumDomainEditDistance;
    private readonly int _maximumInputLength;
    private readonly EmailUsage _emailUsage;
    private readonly EmailProtectionLevel _emailProtectionLevel;

    /// <summary>Creates an email checker with default email options.</summary>
    public EmailChecker()
        : this(new EmailOptions())
    {
    }

    /// <summary>Creates an email checker from captured email options and an email-adapted Unclaimable checker.</summary>
    public EmailChecker(EmailOptions options)
        : this(
            options is null
                ? throw new ArgumentNullException(nameof(options))
                : new global::Unclaimable.Checker(options.LocalPartOptions),
            options)
    {
    }

    /// <summary>
    /// Creates an email checker with an explicitly supplied local-part checker.
    /// Domain settings are captured from <paramref name="options"/> at construction time.
    /// </summary>
    public EmailChecker(global::Unclaimable.IChecker localPartChecker, EmailOptions options)
    {
        _localPartChecker = localPartChecker ?? throw new ArgumentNullException(nameof(localPartChecker));

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.MaximumDomainEditDistance < 0 || options.MaximumDomainEditDistance > 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaximumDomainEditDistance),
                "MaximumDomainEditDistance must be between 0 and 2.");
        }

        if (!Enum.IsDefined(typeof(EmailUsage), options.EmailUsage))
        {
            throw new ArgumentOutOfRangeException(nameof(options.EmailUsage));
        }
        if (!Enum.IsDefined(typeof(EmailProtectionLevel), options.EmailProtectionLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(options.EmailProtectionLevel));
        }
        if (options.MaximumInputLength < 1)
            throw new ArgumentOutOfRangeException(nameof(options.MaximumInputLength));
        _maximumInputLength = options.MaximumInputLength;
        _emailUsage = options.EmailUsage;
        _emailProtectionLevel = options.EmailProtectionLevel;
        _allowIssuingDomainSubdomains = options.AllowIssuingDomainSubdomains;
        _enforceIssuingDomainsForExistingAddresses = options.EnforceIssuingDomainsForExistingAddresses;
        _detectUnicodeLookalikes = options.DetectUnicodeLookalikes;
        _detectTypographicalLookalikes = options.DetectTypographicalLookalikes;
        _detectProtectedLabelReuse = options.DetectProtectedLabelReuse;
        _maximumDomainEditDistance = options.MaximumDomainEditDistance;

        _issuingDomains = NormalizeConfiguredDomains(options.IssuingDomains, nameof(options.IssuingDomains));

        var protectedDomains = new Dictionary<string, ProtectedDomain>(StringComparer.Ordinal);
        AddConfiguredProtectedDomains(protectedDomains, options.ProtectedDomains, nameof(options.ProtectedDomains));
        AddConfiguredProtectedDomains(protectedDomains, options.IssuingDomains, nameof(options.IssuingDomains));

        _protectedDomains = new ProtectedDomain[protectedDomains.Count];
        protectedDomains.Values.CopyTo(_protectedDomains, 0);
    }

    /// <inheritdoc />
    public bool IsAllowed(string? address, EmailAddressPurpose purpose) =>
        Check(address, purpose).IsAllowed;

    /// <inheritdoc />
    public EmailResult CheckExistingAddress(string? address) =>
        Check(address, EmailAddressPurpose.ExistingAddress);

    /// <inheritdoc />
    public EmailResult CheckNewAddress(string? address) =>
        Check(address, EmailAddressPurpose.NewAddress);

    /// <inheritdoc />
    public EmailResult Check(string? address, EmailAddressPurpose purpose)
    {
        if (purpose != EmailAddressPurpose.ExistingAddress
            && purpose != EmailAddressPurpose.NewAddress)
        {
            throw new ArgumentOutOfRangeException(nameof(purpose));
        }

        var effectivePurpose = _emailUsage == EmailUsage.Auto
            ? purpose
            : (_emailUsage == EmailUsage.IssuedAddress
                ? EmailAddressPurpose.NewAddress
                : EmailAddressPurpose.ExistingAddress);

        if (address != null && address.Length > _maximumInputLength)
        {
            return new EmailResult(address, purpose, null, null,
                EmailFailureKind.InvalidFormat, null, DomainLookalikeKind.None, null,
                effectivePurpose: effectivePurpose);
        }

        ParsedAddress? parsed;
        EmailFailureKind syntaxFailure;
        string? localPart;
        if (!TryParseAddress(address, out parsed, out syntaxFailure, out localPart))
        {
            return new EmailResult(
                address,
                purpose,
                localPart,
                null,
                syntaxFailure,
                null,
                DomainLookalikeKind.None,
                null,
                effectivePurpose: effectivePurpose);
        }

        var relaxedExisting = _emailProtectionLevel == EmailProtectionLevel.Relaxed
            && effectivePurpose == EmailAddressPurpose.ExistingAddress;
        var localPartResult = _localPartChecker.Check(parsed!.LocalPart);
        var domainAssessment = AssessDomain(parsed.OriginalDomain, parsed.Domain);
        var enforceIssuingDomains =
            effectivePurpose == EmailAddressPurpose.NewAddress
            || _enforceIssuingDomainsForExistingAddresses;
        var approvedIssuingDomain =
            !enforceIssuingDomains
            || _issuingDomains.Length == 0
            || IsWithinConfiguredDomain(parsed.Domain, _issuingDomains, _allowIssuingDomainSubdomains);

        EmailFailureKind failureKind;
        if (localPartResult.IsReserved && !relaxedExisting)
        {
            failureKind = EmailFailureKind.ReservedLocalPart;
        }
        else if (domainAssessment.Kind != DomainLookalikeKind.None && !relaxedExisting)
        {
            failureKind = EmailFailureKind.SuspiciousDomain;
        }
        else if (!approvedIssuingDomain)
        {
            failureKind = EmailFailureKind.UnapprovedIssuingDomain;
        }
        else
        {
            failureKind = EmailFailureKind.None;
        }

        return new EmailResult(
            address,
            purpose,
            parsed.LocalPart,
            parsed.Domain,
            failureKind,
            localPartResult,
            domainAssessment.Kind,
            domainAssessment.MatchedProtectedDomain,
            approvedIssuingDomain,
            effectivePurpose);
    }

    private DomainAssessment AssessDomain(string originalDomain, string normalizedDomain)
    {
        if (_protectedDomains.Length == 0)
        {
            return DomainAssessment.Safe;
        }

        // Trust of one configured domain must not suppress impersonation of
        // another configured domain elsewhere in the same hostname.
        foreach (var protectedDomain in _protectedDomains)
        {
            if (normalizedDomain.StartsWith(protectedDomain.Domain + ".", StringComparison.Ordinal))
            {
                return new DomainAssessment(
                    DomainLookalikeKind.EmbeddedProtectedDomain,
                    protectedDomain.Domain);
            }
        }

        // A genuine subdomain of a configured protected domain remains trusted
        // unless it embeds another protected domain's name (checked above).
        foreach (var protectedDomain in _protectedDomains)
        {
            if (IsSameOrSubdomain(normalizedDomain, protectedDomain.Domain))
            {
                return DomainAssessment.Safe;
            }
        }

        var candidateLabels = normalizedDomain.Split('.');

        if (_detectUnicodeLookalikes)
        {
            foreach (var protectedDomain in _protectedDomains)
            {
                for (var labelIndex = 0; labelIndex < candidateLabels.Length; labelIndex++)
                {
                    var candidateLabel = candidateLabels[labelIndex];
                    if (string.Equals(candidateLabel, protectedDomain.RegistrantLabel, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (AreDomainLabelsVisuallyEquivalent(
                        candidateLabel,
                        protectedDomain.RegistrantLabel,
                        protectedDomain.RegistrantSkeleton))
                    {
                        return new DomainAssessment(
                            DomainLookalikeKind.Confusable,
                            protectedDomain.Domain);
                    }
                }
            }
        }

        if (_detectTypographicalLookalikes && _maximumDomainEditDistance > 0)
        {
            foreach (var protectedDomain in _protectedDomains)
            {
                for (var labelIndex = 0; labelIndex < candidateLabels.Length; labelIndex++)
                {
                    var candidateLabel = candidateLabels[labelIndex];
                    if (string.Equals(candidateLabel, protectedDomain.RegistrantLabel, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (IsWithinDamerauLevenshteinDistance(
                        candidateLabel,
                        protectedDomain.RegistrantLabel,
                        _maximumDomainEditDistance))
                    {
                        return new DomainAssessment(
                            DomainLookalikeKind.Typographical,
                            protectedDomain.Domain);
                    }
                }
            }
        }

        if (_detectProtectedLabelReuse)
        {
            foreach (var protectedDomain in _protectedDomains)
            {
                if (ReusesProtectedRegistrantLabel(normalizedDomain, protectedDomain.RegistrantLabel))
                {
                    return new DomainAssessment(
                        DomainLookalikeKind.ProtectedLabelReuse,
                        protectedDomain.Domain);
                }
            }
        }

        return DomainAssessment.Safe;
    }

    private static bool TryParseAddress(
        string? address,
        out ParsedAddress? parsed,
        out EmailFailureKind failureKind,
        out string? localPart)
    {
        parsed = null;
        localPart = null;

        if (address is null || address.Length == 0)
        {
            failureKind = EmailFailureKind.InvalidFormat;
            return false;
        }

        if (!string.Equals(address, address.Trim(), StringComparison.Ordinal))
        {
            failureKind = EmailFailureKind.InvalidFormat;
            return false;
        }

        var at = address.IndexOf('@');
        if (at <= 0 || at != address.LastIndexOf('@') || at == address.Length - 1)
        {
            failureKind = EmailFailureKind.InvalidFormat;
            return false;
        }

        localPart = address.Substring(0, at);
        var originalDomain = address.Substring(at + 1);

        if (!IsValidLocalPart(localPart))
        {
            failureKind = EmailFailureKind.InvalidLocalPart;
            return false;
        }

        string normalizedDomain;
        if (!TryNormalizeDomain(originalDomain, out normalizedDomain))
        {
            failureKind = EmailFailureKind.InvalidDomain;
            return false;
        }

        var totalLength =
            Encoding.UTF8.GetByteCount(localPart)
            + 1
            + normalizedDomain.Length;

        if (totalLength > 254)
        {
            failureKind = EmailFailureKind.InvalidFormat;
            return false;
        }

        parsed = new ParsedAddress(localPart, originalDomain, normalizedDomain);
        failureKind = EmailFailureKind.None;
        return true;
    }

    private static bool IsValidLocalPart(string localPart)
    {
        if (localPart.Length == 0
            || localPart[0] == '.'
            || localPart[localPart.Length - 1] == '.'
            || localPart.IndexOf("..", StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        if (Encoding.UTF8.GetByteCount(localPart) > 64)
        {
            return false;
        }

        for (var index = 0; index < localPart.Length; index++)
        {
            var character = localPart[index];

            if (char.IsHighSurrogate(character))
            {
                if (index + 1 >= localPart.Length || !char.IsLowSurrogate(localPart[index + 1]))
                {
                    return false;
                }

                var category = CharUnicodeInfo.GetUnicodeCategory(localPart, index);
                if (!IsAllowedInternationalLocalCategory(category))
                {
                    return false;
                }

                index++;
                continue;
            }

            if (char.IsLowSurrogate(character))
            {
                return false;
            }

            if (character <= 0x7F)
            {
                if (character != '.' && !IsAsciiAtext(character))
                {
                    return false;
                }

                continue;
            }

            if (char.IsWhiteSpace(character)
                || !IsAllowedInternationalLocalCategory(CharUnicodeInfo.GetUnicodeCategory(character)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiAtext(char character)
    {
        if ((character >= 'a' && character <= 'z')
            || (character >= 'A' && character <= 'Z')
            || (character >= '0' && character <= '9'))
        {
            return true;
        }

        switch (character)
        {
            case '!':
            case '#':
            case '$':
            case '%':
            case '&':
            case '\'':
            case '*':
            case '+':
            case '-':
            case '/':
            case '=':
            case '?':
            case '^':
            case '_':
            case '{':
            case '|':
            case '}':
            case '~':
                return true;
            default:
                return character == (char)0x0060;
        }
    }

    private static bool IsAllowedInternationalLocalCategory(UnicodeCategory category)
    {
        return category != UnicodeCategory.Control
            && category != UnicodeCategory.Format
            && category != UnicodeCategory.LineSeparator
            && category != UnicodeCategory.ParagraphSeparator
            && category != UnicodeCategory.SpaceSeparator
            && category != UnicodeCategory.Surrogate
            && category != UnicodeCategory.OtherNotAssigned
            && category != UnicodeCategory.PrivateUse;
    }

    private static string[] NormalizeConfiguredDomains(IEnumerable<string> domains, string parameterName)
    {
        var unique = new HashSet<string>(StringComparer.Ordinal);

        foreach (var domain in domains)
        {
            string normalized;
            if (string.IsNullOrWhiteSpace(domain) || !TryNormalizeDomain(domain, out normalized))
            {
                throw new ArgumentException(
                    "Configured domains must be valid DNS-style domains with a plausible top-level domain.",
                    parameterName);
            }

            unique.Add(normalized);
        }

        var result = new string[unique.Count];
        unique.CopyTo(result);
        return result;
    }

    private static void AddConfiguredProtectedDomains(
        Dictionary<string, ProtectedDomain> destination,
        IEnumerable<string> domains,
        string parameterName)
    {
        foreach (var domain in domains)
        {
            string normalized;
            if (string.IsNullOrWhiteSpace(domain) || !TryNormalizeDomain(domain, out normalized))
            {
                throw new ArgumentException(
                    "Configured domains must be valid DNS-style domains with a plausible top-level domain.",
                    parameterName);
            }

            if (!destination.ContainsKey(normalized))
            {
                destination.Add(normalized, new ProtectedDomain(normalized));
            }
        }
    }

    private static bool TryNormalizeDomain(string domain, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrEmpty(domain)
            || domain[0] == '.'
            || domain[domain.Length - 1] == '.')
        {
            return false;
        }

        var sourceLabels = domain.Split('.');
        if (sourceLabels.Length < 2)
        {
            return false;
        }

        var asciiLabels = new string[sourceLabels.Length];
        var idn = new IdnMapping
        {
            UseStd3AsciiRules = true
        };

        try
        {
            for (var index = 0; index < sourceLabels.Length; index++)
            {
                var sourceLabel = sourceLabels[index];
                if (sourceLabel.Length == 0)
                {
                    return false;
                }

                var asciiLabel = idn.GetAscii(sourceLabel).ToLowerInvariant();
                if (!IsValidAsciiDomainLabel(asciiLabel))
                {
                    return false;
                }

                asciiLabels[index] = asciiLabel;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        var topLevelDomain = asciiLabels[asciiLabels.Length - 1];
        if (!IsPlausibleTopLevelDomain(topLevelDomain))
        {
            return false;
        }

        normalized = string.Join(".", asciiLabels);
        return normalized.Length <= 253;
    }

    private static bool IsValidAsciiDomainLabel(string label)
    {
        if (label.Length < 1
            || label.Length > 63
            || label[0] == '-'
            || label[label.Length - 1] == '-')
        {
            return false;
        }

        for (var index = 0; index < label.Length; index++)
        {
            var character = label[index];
            if ((character >= 'a' && character <= 'z')
                || (character >= '0' && character <= '9')
                || character == '-')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsPlausibleTopLevelDomain(string topLevelDomain)
    {
        if (topLevelDomain.StartsWith("xn--", StringComparison.Ordinal))
        {
            return topLevelDomain.Length > 4;
        }

        if (topLevelDomain.Length < 2)
        {
            return false;
        }

        for (var index = 0; index < topLevelDomain.Length; index++)
        {
            var character = topLevelDomain[index];
            if (character < 'a' || character > 'z')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsWithinConfiguredDomain(
        string candidate,
        string[] configuredDomains,
        bool allowSubdomains)
    {
        for (var index = 0; index < configuredDomains.Length; index++)
        {
            if (string.Equals(candidate, configuredDomains[index], StringComparison.Ordinal)
                || (allowSubdomains && IsSameOrSubdomain(candidate, configuredDomains[index])))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameOrSubdomain(string candidate, string configuredDomain)
    {
        return string.Equals(candidate, configuredDomain, StringComparison.Ordinal)
            || (candidate.Length > configuredDomain.Length
                && candidate.EndsWith("." + configuredDomain, StringComparison.Ordinal));
    }

    private static string GetRegistrantLabel(string domain) =>
        PublicSuffixResolver.GetRegistrantLabel(domain);

    private static bool AreDomainLabelsVisuallyEquivalent(
        string candidateLabel,
        string protectedLabel,
        string protectedSkeleton)
    {
        string candidateUnicode;
        try
        {
            candidateUnicode = new IdnMapping().GetUnicode(candidateLabel);
        }
        catch (ArgumentException)
        {
            // A platform-specific IDNA decoding failure must not make a
            // syntactically validated mailbox crash during lookalike checks.
            return false;
        }
        bool changed;
        var candidateSkeleton = global::Unclaimable.ConfusableNormalizer.CreateSkeleton(
            candidateUnicode,
            includeAsciiObfuscation: false,
            out changed);

        if (string.Equals(candidateSkeleton, protectedSkeleton, StringComparison.Ordinal))
        {
            return changed || !string.Equals(candidateLabel, protectedLabel, StringComparison.Ordinal);
        }

        return TryMatchDomainVisual(candidateSkeleton, 0, protectedSkeleton, 0);
    }

    private static bool TryMatchDomainVisual(string candidate, int candidateIndex, string target, int targetIndex)
    {
        while (candidateIndex < candidate.Length && targetIndex < target.Length)
        {
            var candidateCharacter = candidate[candidateIndex];
            var targetCharacter = target[targetIndex];

            if (candidateCharacter == targetCharacter)
            {
                candidateIndex++;
                targetIndex++;
                continue;
            }

            string[]? substitutions;
            if (global::Unclaimable.ConfusableNormalizer.TryGetObfuscationSubstitutions(
                    candidateCharacter,
                    out substitutions))
            {
                for (var index = 0; index < substitutions!.Length; index++)
                {
                    if (substitutions[index].Length == 1
                        && substitutions[index][0] == targetCharacter)
                    {
                        candidateIndex++;
                        targetIndex++;
                        goto ContinueMatching;
                    }
                }
            }

            if (targetCharacter == 'm'
                && candidateIndex + 1 < candidate.Length
                && candidate[candidateIndex] == 'r'
                && candidate[candidateIndex + 1] == 'n')
            {
                candidateIndex += 2;
                targetIndex++;
                continue;
            }

            if (targetCharacter == 'w'
                && candidateIndex + 1 < candidate.Length
                && candidate[candidateIndex] == 'v'
                && candidate[candidateIndex + 1] == 'v')
            {
                candidateIndex += 2;
                targetIndex++;
                continue;
            }

            if (targetCharacter == 'd'
                && candidateIndex + 1 < candidate.Length
                && candidate[candidateIndex] == 'c'
                && candidate[candidateIndex + 1] == 'l')
            {
                candidateIndex += 2;
                targetIndex++;
                continue;
            }

            return false;

        ContinueMatching:
            continue;
        }

        return candidateIndex == candidate.Length && targetIndex == target.Length;
    }

    private static bool ReusesProtectedRegistrantLabel(string candidateDomain, string protectedLabel)
    {
        if (protectedLabel.Length < 3)
        {
            return false;
        }

        var labels = candidateDomain.Split('.');
        for (var index = 0; index < labels.Length; index++)
        {
            var label = labels[index];

            if (string.Equals(label, protectedLabel, StringComparison.Ordinal))
            {
                return true;
            }

            var segments = label.Split('-');
            for (var segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                if (string.Equals(segments[segmentIndex], protectedLabel, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string CreateNormalizedDomainSkeleton(string normalizedAsciiDomain)
    {
        var unicodeDomain = new IdnMapping().GetUnicode(normalizedAsciiDomain);
        bool changed;
        return global::Unclaimable.ConfusableNormalizer.CreateSkeleton(
            unicodeDomain,
            includeAsciiObfuscation: true,
            out changed);
    }

    private static bool IsWithinDamerauLevenshteinDistance(
        string left,
        string right,
        int maximumDistance)
    {
        if (Math.Abs(left.Length - right.Length) > maximumDistance)
        {
            return false;
        }

        return IsWithinBoundedEditDistance(
            left,
            0,
            right,
            0,
            maximumDistance);
    }

    private static bool IsWithinBoundedEditDistance(
        string left,
        int leftIndex,
        string right,
        int rightIndex,
        int editsRemaining)
    {
        while (leftIndex < left.Length
               && rightIndex < right.Length
               && left[leftIndex] == right[rightIndex])
        {
            leftIndex++;
            rightIndex++;
        }

        if (leftIndex == left.Length || rightIndex == right.Length)
        {
            return Math.Abs(
                (left.Length - leftIndex) - (right.Length - rightIndex))
                <= editsRemaining;
        }

        if (editsRemaining == 0)
        {
            return false;
        }

        var nextBudget = editsRemaining - 1;

        if (IsWithinBoundedEditDistance(
                left,
                leftIndex + 1,
                right,
                rightIndex + 1,
                nextBudget))
        {
            return true;
        }

        if (IsWithinBoundedEditDistance(
                left,
                leftIndex + 1,
                right,
                rightIndex,
                nextBudget))
        {
            return true;
        }

        if (IsWithinBoundedEditDistance(
                left,
                leftIndex,
                right,
                rightIndex + 1,
                nextBudget))
        {
            return true;
        }

        return leftIndex + 1 < left.Length
               && rightIndex + 1 < right.Length
               && left[leftIndex] == right[rightIndex + 1]
               && left[leftIndex + 1] == right[rightIndex]
               && IsWithinBoundedEditDistance(
                   left,
                   leftIndex + 2,
                   right,
                   rightIndex + 2,
                   nextBudget);
    }

}
