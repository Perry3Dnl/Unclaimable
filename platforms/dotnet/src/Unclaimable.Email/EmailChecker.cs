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
        public ProtectedDomain(string domain, string skeleton)
        {
            Domain = domain;
            Skeleton = skeleton;

            var separator = domain.IndexOf('.');
            RegistrantLabel = separator < 0 ? domain : domain.Substring(0, separator);
        }

        public string Domain { get; }
        public string Skeleton { get; }
        public string RegistrantLabel { get; }
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
    private readonly bool _detectUnicodeLookalikes;
    private readonly bool _detectTypographicalLookalikes;
    private readonly bool _detectProtectedLabelReuse;
    private readonly int _maximumDomainEditDistance;

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
                null);
        }

        var localPartResult = _localPartChecker.Check(parsed!.LocalPart);
        var domainAssessment = AssessDomain(parsed.OriginalDomain, parsed.Domain);
        var approvedIssuingDomain =
            purpose != EmailAddressPurpose.NewAddress
            || _issuingDomains.Length == 0
            || IsWithinConfiguredDomain(parsed.Domain, _issuingDomains);

        EmailFailureKind failureKind;
        if (localPartResult.IsReserved)
        {
            failureKind = EmailFailureKind.ReservedLocalPart;
        }
        else if (domainAssessment.Kind != DomainLookalikeKind.None)
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
            domainAssessment.MatchedProtectedDomain);
    }

    private DomainAssessment AssessDomain(string originalDomain, string normalizedDomain)
    {
        if (_protectedDomains.Length == 0)
        {
            return DomainAssessment.Safe;
        }

        foreach (var protectedDomain in _protectedDomains)
        {
            if (IsSameOrSubdomain(normalizedDomain, protectedDomain.Domain))
            {
                return DomainAssessment.Safe;
            }
        }

        foreach (var protectedDomain in _protectedDomains)
        {
            if (normalizedDomain.StartsWith(protectedDomain.Domain + ".", StringComparison.Ordinal))
            {
                return new DomainAssessment(
                    DomainLookalikeKind.EmbeddedProtectedDomain,
                    protectedDomain.Domain);
            }
        }

        if (_detectUnicodeLookalikes)
        {
            var candidateSkeleton = CreateDomainSkeleton(originalDomain);
            foreach (var protectedDomain in _protectedDomains)
            {
                if (string.Equals(candidateSkeleton, protectedDomain.Skeleton, StringComparison.Ordinal))
                {
                    return new DomainAssessment(
                        DomainLookalikeKind.Confusable,
                        protectedDomain.Domain);
                }
            }
        }

        if (_detectTypographicalLookalikes && _maximumDomainEditDistance > 0)
        {
            foreach (var protectedDomain in _protectedDomains)
            {
                if (IsWithinDamerauLevenshteinDistance(
                    normalizedDomain,
                    protectedDomain.Domain,
                    _maximumDomainEditDistance))
                {
                    return new DomainAssessment(
                        DomainLookalikeKind.Typographical,
                        protectedDomain.Domain);
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

        if (string.IsNullOrEmpty(address)
            || !string.Equals(address, address.Trim(), StringComparison.Ordinal))
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
                destination.Add(
                    normalized,
                    new ProtectedDomain(normalized, CreateDomainSkeleton(domain)));
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

    private static bool IsWithinConfiguredDomain(string candidate, string[] configuredDomains)
    {
        for (var index = 0; index < configuredDomains.Length; index++)
        {
            if (IsSameOrSubdomain(candidate, configuredDomains[index]))
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

    private static string CreateDomainSkeleton(string domain)
    {
        var decomposed = domain
            .Normalize(NormalizationForm.FormD)
            .ToLowerInvariant();

        var builder = new StringBuilder(decomposed.Length);

        for (var index = 0; index < decomposed.Length; index++)
        {
            var character = decomposed[index];
            var category = CharUnicodeInfo.GetUnicodeCategory(decomposed, index);

            if (category == UnicodeCategory.NonSpacingMark
                || category == UnicodeCategory.SpacingCombiningMark
                || category == UnicodeCategory.EnclosingMark)
            {
                continue;
            }

            char mapped;
            if (TryMapDomainConfusable(character, out mapped))
            {
                builder.Append(mapped);
            }
            else
            {
                builder.Append(character);
            }

            if (char.IsHighSurrogate(character)
                && index + 1 < decomposed.Length
                && char.IsLowSurrogate(decomposed[index + 1]))
            {
                builder.Append(decomposed[index + 1]);
                index++;
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool TryMapDomainConfusable(char character, out char mapped)
    {
        switch (character)
        {
            case '0': mapped = 'o'; return true;
            case '1': mapped = 'l'; return true;
            case '3': mapped = 'e'; return true;
            case '5': mapped = 's'; return true;
            case '7': mapped = 't'; return true;
            case '8': mapped = 'b'; return true;
            case (char)0x0430: mapped = 'a'; return true;
            case (char)0x0432: mapped = 'b'; return true;
            case (char)0x0435: mapped = 'e'; return true;
            case (char)0x043A: mapped = 'k'; return true;
            case (char)0x043C: mapped = 'm'; return true;
            case (char)0x043D: mapped = 'h'; return true;
            case (char)0x043E: mapped = 'o'; return true;
            case (char)0x0440: mapped = 'p'; return true;
            case (char)0x0441: mapped = 'c'; return true;
            case (char)0x0442: mapped = 't'; return true;
            case (char)0x0443: mapped = 'y'; return true;
            case (char)0x0445: mapped = 'x'; return true;
            case (char)0x0455: mapped = 's'; return true;
            case (char)0x0456: mapped = 'i'; return true;
            case (char)0x0458: mapped = 'j'; return true;
            case (char)0x04CF: mapped = 'l'; return true;
            case (char)0x03B1: mapped = 'a'; return true;
            case (char)0x03B2: mapped = 'b'; return true;
            case (char)0x03B5: mapped = 'e'; return true;
            case (char)0x03B9: mapped = 'i'; return true;
            case (char)0x03BA: mapped = 'k'; return true;
            case (char)0x03BC: mapped = 'm'; return true;
            case (char)0x03BD: mapped = 'v'; return true;
            case (char)0x03BF: mapped = 'o'; return true;
            case (char)0x03C1: mapped = 'p'; return true;
            case (char)0x03C2: mapped = 'c'; return true;
            case (char)0x03C4: mapped = 't'; return true;
            case (char)0x03C5: mapped = 'y'; return true;
            case (char)0x03C7: mapped = 'x'; return true;
            case (char)0x0131: mapped = 'i'; return true;
            default:
                mapped = (char)0;
                return false;
        }
    }

    private static bool IsWithinDamerauLevenshteinDistance(string left, string right, int maximumDistance)
    {
        if (string.Equals(left, right, StringComparison.Ordinal))
        {
            return true;
        }

        if (Math.Abs(left.Length - right.Length) > maximumDistance)
        {
            return false;
        }

        var previousPrevious = new int[right.Length + 1];
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];

        for (var column = 0; column <= right.Length; column++)
        {
            previous[column] = column;
        }

        for (var row = 1; row <= left.Length; row++)
        {
            current[0] = row;

            for (var column = 1; column <= right.Length; column++)
            {
                var substitutionCost = left[row - 1] == right[column - 1] ? 0 : 1;
                var deletion = previous[column] + 1;
                var insertion = current[column - 1] + 1;
                var substitution = previous[column - 1] + substitutionCost;
                var distance = Math.Min(Math.Min(deletion, insertion), substitution);

                if (row > 1
                    && column > 1
                    && left[row - 1] == right[column - 2]
                    && left[row - 2] == right[column - 1])
                {
                    distance = Math.Min(distance, previousPrevious[column - 2] + 1);
                }

                current[column] = distance;
            }

            var temporary = previousPrevious;
            previousPrevious = previous;
            previous = current;
            current = temporary;
        }

        return previous[right.Length] <= maximumDistance;
    }
}
