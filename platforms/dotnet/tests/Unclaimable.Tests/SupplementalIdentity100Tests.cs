using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Unclaimable.Extended;
using Xunit;

namespace Unclaimable.Tests;

public sealed class SupplementalIdentity100Tests
{
    [Fact]
    public void DataIsNotEnabledByDefault()
    {
        var options = new Options();
        Assert.Empty(options.AdditionalReserved);
        Assert.True(new Checker(options).IsClaimable("bluegarden"));
        Assert.True(new Checker(ValidationPresets.Username()).IsClaimable("bluegarden"));
    }

    [Fact]
    public void RegistrationUsesCoreAndKeepsOrdinaryChecksUnchanged()
    {
        var options = new Options();
        options.UseSupplementalIdentityData();
        var checker = new Checker(options);

        var result = checker.Check("Bitfinex");
        Assert.True(result.IsReserved);
        Assert.Equal("identity-crypto-exchanges", result.Category);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal(new Checker().IsClaimable("bluegarden"), checker.IsClaimable("bluegarden"));
    }

    [Fact]
    public void HighCollisionGenericTermsAreNotInSupplementalSnapshot()
    {
        var assembly = typeof(SupplementalIdentityOptionsExtensions).Assembly;
        using var stream = assembly.GetManifestResourceStream("Unclaimable.Extended.Vocabulary.identity.json");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var snapshot = reader.ReadToEnd();
        Assert.DoesNotContain("\"Safe\"", snapshot);
        Assert.DoesNotContain("\"Flow\"", snapshot);
        Assert.DoesNotContain("\"Gala\"", snapshot);
        Assert.DoesNotContain("\"Rain\"", snapshot);
    }

    [Fact]
    public void OptInRegistrationHandlesNullAndSeparateOptions()
    {
        Assert.Throws<ArgumentNullException>(() => SupplementalIdentityOptionsExtensions.UseSupplementalIdentityData(null!));
        var defaultOptions = new Options();
        var optedIn = new Options().UseSupplementalIdentityData();
        Assert.True(new Checker(defaultOptions).IsClaimable("bluegarden"));
        Assert.True(new Checker(optedIn).IsClaimable("bluegarden"));
        Assert.Equal(3, defaultOptions.MinimumLength);
        Assert.Equal(3, optedIn.MinimumLength);
    }

    [Fact]
    public void ExplicitRegistrationDoesNotChangeExistingExtendedMethod()
    {
        var original = new Options().UseExtendedData();
        var extra = new Options().UseExtendedData().UseSupplementalIdentityData();
        var originalChecker = new Checker(original);
        var extraChecker = new Checker(extra);
        Assert.Equal(originalChecker.IsClaimable("bluegarden"), extraChecker.IsClaimable("bluegarden"));
    }
    [Fact]
    public void EverySupplementalEntryIsOnlyActivatedForWholeIdentifiers()
    {
        const string resourceName = "Unclaimable.Extended.Vocabulary.identity.json";
        using var stream = typeof(SupplementalIdentityOptionsExtensions).Assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);
        using var document = JsonDocument.Parse(stream!);
        Assert.Equal(1, document.RootElement.GetProperty("schema").GetInt32());

        var groups = document.RootElement.GetProperty("groups").EnumerateArray().ToArray();
        Assert.Equal(6, groups.Length);

        var core = new Checker(new Options());
        var supplemental = new Checker(new Options().UseSupplementalIdentityData());
        var count = 0;
        var unique = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var category = group.GetProperty("category").GetString();
            Assert.False(string.IsNullOrWhiteSpace(category));

            foreach (var entry in group.GetProperty("values").EnumerateArray())
            {
                var word = entry.GetString();
                Assert.False(string.IsNullOrWhiteSpace(word));
                Assert.True(word!.Length >= 8, "Unexpected short source entry: " + word);
                Assert.True(unique.Add(word.Normalize(System.Text.NormalizationForm.FormKC).ToLowerInvariant()),
                    "Duplicate normalized supplemental entry: " + word);

                // Preserve existing failure precedence, even for words containing spaces:
                // supplemental registration must not make an accepted value claimable.
                Assert.True(supplemental.IsReserved(word));

                // Exact reservations must not create new generic substring matches.
                // Equal outcomes guarantee no behavior drift on these two safe wrappers.
                var prefixed = "bluegarden" + word;
                var suffixed = word + "bluegarden";
                Assert.Equal(core.IsClaimable(prefixed), supplemental.IsClaimable(prefixed));
                Assert.Equal(core.IsClaimable(suffixed), supplemental.IsClaimable(suffixed));
                count++;
            }
        }

        Assert.Equal(410, count);
    }

}
