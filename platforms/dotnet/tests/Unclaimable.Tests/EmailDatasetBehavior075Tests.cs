using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

/// <summary>
/// Proves protected-domain behavior from the built-in brand/technology data instead of
/// relying only on a small set of hand-picked domains. New domain-safe dataset values
/// automatically become test inputs.
/// </summary>
public sealed class EmailDatasetBehavior075Tests
{
    private static readonly string[] DatasetFiles =
    {
        "email-dataset-brands-core.json",
        "email-dataset-brands-extended.json",
        "email-dataset-technology-core.json",
        "email-dataset-technology-extended.json"
    };

    private sealed class Dataset
    {
        [JsonPropertyName("values")]
        public string[] Values { get; init; } = Array.Empty<string>();
    }

    public static IEnumerable<object[]> ProtectedLabels()
    {
        return LoadProtectedLabels().Select(label => new object[] { label });
    }

    [Theory]
    [MemberData(nameof(ProtectedLabels))]
    public void EveryDatasetLabelGetsTheSameGenericDomainProtection(string label)
    {
        var protectedDomain = label + ".com";
        var checker = CreateProtectedChecker(protectedDomain);

        // Legitimate ownership paths remain allowed.
        AssertAllowed(checker, label + ".com");
        AssertAllowed(checker, label.ToUpperInvariant() + ".COM");
        AssertAllowed(checker, "mail." + label + ".com");
        AssertAllowed(checker, "deep.mail." + label + ".com");

        // One-edit typo families are algorithmic, not brand-specific.
        AssertSuspicious(
            checker,
            DeleteOne(label) + ".com",
            protectedDomain,
            DomainLookalikeKind.Typographical);

        AssertSuspicious(
            checker,
            InsertOne(label) + ".com",
            protectedDomain,
            DomainLookalikeKind.Typographical);

        AssertSuspicious(
            checker,
            SubstituteOne(label) + ".com",
            protectedDomain,
            DomainLookalikeKind.Typographical);

        var transposed = TransposeOne(label);
        if (transposed is not null)
        {
            AssertSuspicious(
                checker,
                transposed + ".com",
                protectedDomain,
                DomainLookalikeKind.Typographical);
        }

        // Reusing the exact protected label elsewhere remains suspicious.
        AssertSuspicious(
            checker,
            label + ".net",
            protectedDomain,
            DomainLookalikeKind.ProtectedLabelReuse);

        AssertSuspicious(
            checker,
            label + "-login.com",
            protectedDomain,
            DomainLookalikeKind.ProtectedLabelReuse);

        AssertSuspicious(
            checker,
            "login-" + label + ".com",
            protectedDomain,
            DomainLookalikeKind.ProtectedLabelReuse);

        // A real protected domain embedded at the start of a longer attacker domain
        // must not be mistaken for a subdomain of the protected domain.
        AssertSuspicious(
            checker,
            protectedDomain + ".attacker.com",
            protectedDomain,
            DomainLookalikeKind.EmbeddedProtectedDomain);
    }

    [Theory]
    [MemberData(nameof(ProtectedLabels))]
    public void DatasetLabelsUseAsciiConfusableDetectionWheneverApplicable(string label)
    {
        var candidate = CreateAsciiConfusable(label);
        if (candidate is null)
        {
            return;
        }

        var protectedDomain = label + ".com";
        AssertSuspicious(
            CreateProtectedChecker(protectedDomain),
            candidate + ".com",
            protectedDomain,
            DomainLookalikeKind.Confusable);
    }

    [Theory]
    [MemberData(nameof(ProtectedLabels))]
    public void DatasetLabelsUseUnicodeAndPunycodeConfusableDetectionWheneverApplicable(string label)
    {
        var unicodeCandidate = CreateUnicodeConfusable(label);
        if (unicodeCandidate is null)
        {
            return;
        }

        var protectedDomain = label + ".com";
        var checker = CreateProtectedChecker(protectedDomain);

        AssertSuspicious(
            checker,
            unicodeCandidate + ".com",
            protectedDomain,
            DomainLookalikeKind.Confusable);

        var punycodeCandidate = new IdnMapping().GetAscii(unicodeCandidate + ".com");
        AssertSuspicious(
            checker,
            punycodeCandidate,
            protectedDomain,
            DomainLookalikeKind.Confusable);
    }

    [Fact]
    public void DatasetDrivenCoverageIsBroadAndIncludesExpectedReferenceBrands()
    {
        var labels = LoadProtectedLabels();

        Assert.True(labels.Count >= 500, $"Expected at least 500 reusable protected labels, found {labels.Count}.");
        Assert.Contains("mcdonalds", labels);
        Assert.Contains("nike", labels);
        Assert.Contains("google", labels);
        Assert.Contains("amazon", labels);
        Assert.Contains("visa", labels);
        Assert.Contains("nvidia", labels);
        Assert.Contains("lidl", labels);
        Assert.Contains("paypal", labels);
        Assert.Contains("microsoft", labels);
        Assert.Contains("netflix", labels);
    }

    private static IReadOnlyList<string> LoadProtectedLabels()
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in DatasetFiles)
        {
            var path = Path.Combine(AppContext.BaseDirectory, file);
            var json = File.ReadAllText(path);
            var dataset = JsonSerializer.Deserialize<Dataset>(json)
                          ?? throw new InvalidOperationException($"Could not load {file}.");

            foreach (var value in dataset.Values)
            {
                var label = ToDomainLabel(value);
                if (label.Length >= 3 && label.Length <= 50)
                {
                    labels.Add(label);
                }
            }
        }

        return labels.OrderBy(value => value, StringComparer.Ordinal).ToArray();
    }

    private static string ToDomainLabel(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.ToLowerInvariant())
        {
            if ((character >= 'a' && character <= 'z')
                || (character >= '0' && character <= '9'))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static string DeleteOne(string label)
    {
        var index = label.Length / 2;
        return label.Remove(index, 1);
    }

    private static string InsertOne(string label)
    {
        var index = label.Length / 2;
        var insertion = label[index] == 'q' ? "x" : "q";
        return label.Insert(index, insertion);
    }

    private static string SubstituteOne(string label)
    {
        var index = label.Length / 2;
        var replacement = label[index] == 'q' ? 'x' : 'q';
        var characters = label.ToCharArray();
        characters[index] = replacement;
        return new string(characters);
    }

    private static string? TransposeOne(string label)
    {
        for (var index = 0; index < label.Length - 1; index++)
        {
            if (label[index] == label[index + 1])
            {
                continue;
            }

            var characters = label.ToCharArray();
            (characters[index], characters[index + 1]) = (characters[index + 1], characters[index]);
            return new string(characters);
        }

        return null;
    }

    private static string? CreateAsciiConfusable(string label)
    {
        var characters = label.ToCharArray();

        for (var index = 0; index < characters.Length; index++)
        {
            switch (characters[index])
            {
                case 'o': characters[index] = '0'; return new string(characters);
                case 'l': characters[index] = '1'; return new string(characters);
                case 'z': characters[index] = '2'; return new string(characters);
                case 'e': characters[index] = '3'; return new string(characters);
                case 'a': characters[index] = '4'; return new string(characters);
                case 's': characters[index] = '5'; return new string(characters);
                case 'g': characters[index] = '9'; return new string(characters);
                case 't': characters[index] = '7'; return new string(characters);
                case 'b': characters[index] = '8'; return new string(characters);
            }
        }

        return null;
    }

    private static string? CreateUnicodeConfusable(string label)
    {
        for (var index = 0; index < label.Length; index++)
        {
            var replacement = label[index] switch
            {
                'a' => "а", // Cyrillic a
                'b' => "β", // Greek beta
                'c' => "с", // Cyrillic es
                'e' => "е", // Cyrillic ie
                'h' => "н", // Cyrillic en
                'i' => "і", // Cyrillic i
                'j' => "ј", // Cyrillic je
                'k' => "к", // Cyrillic ka
                'l' => "ӏ", // Cyrillic palochka
                'm' => "м", // Cyrillic em
                'o' => "о", // Cyrillic o
                'p' => "р", // Cyrillic er
                's' => "ѕ", // Cyrillic dze
                't' => "т", // Cyrillic te
                'x' => "х", // Cyrillic ha
                'y' => "у", // Cyrillic u
                _ => null
            };

            if (replacement is not null)
            {
                return label.Substring(0, index) + replacement + label.Substring(index + 1);
            }
        }

        return null;
    }

    private static EmailChecker CreateProtectedChecker(string protectedDomain)
    {
        var options = new EmailOptions();
        options.ProtectedDomains.Add(protectedDomain);
        return new EmailChecker(options);
    }

    private static void AssertAllowed(EmailChecker checker, string candidateDomain)
    {
        var result = checker.CheckExistingAddress("bluegarden@" + candidateDomain);

        Assert.True(result.IsAllowed, $"{candidateDomain} should be allowed.");
        Assert.Equal(DomainLookalikeKind.None, result.DomainLookalikeKind);
        Assert.Null(result.MatchedProtectedDomain);
    }

    private static void AssertSuspicious(
        EmailChecker checker,
        string candidateDomain,
        string protectedDomain,
        DomainLookalikeKind expectedKind)
    {
        var result = checker.CheckExistingAddress("bluegarden@" + candidateDomain);

        Assert.False(result.IsAllowed);
        Assert.Equal(EmailFailureKind.SuspiciousDomain, result.FailureKind);
        Assert.Equal(expectedKind, result.DomainLookalikeKind);
        Assert.Equal(protectedDomain, result.MatchedProtectedDomain);
    }
}
