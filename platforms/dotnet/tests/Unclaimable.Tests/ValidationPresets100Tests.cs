using Xunit;

namespace Unclaimable.Tests;

public sealed class ValidationPresets100Tests
{
    [Fact]
    public void UsernamePresetPreservesHistoricalDefaults()
    {
        var preset = ValidationPresets.Username();
        var defaults = new Options();
        Assert.Equal(defaults.MinimumLength, preset.MinimumLength);
        Assert.Equal(defaults.MaximumLength, preset.MaximumLength);
        Assert.Equal(defaults.DisabledRules, preset.DisabledRules);
        Assert.Equal(defaults.EnabledPatterns, preset.EnabledPatterns);
        Assert.Equal(defaults.ObfuscationSensitivity, preset.ObfuscationSensitivity);
        Assert.Equal(defaults.FalsePositiveSensitivity, preset.FalsePositiveSensitivity);
        Assert.Equal(new Checker().IsClaimable("bluegarden"), new Checker(preset).IsClaimable("bluegarden"));
    }

    [Fact]
    public void PresetsReturnIndependentOptionsInstances()
    {
        var first = ValidationPresets.Username();
        first.MinimumLength = 20;
        first.AdditionalReserved.Add("bluegarden");
        var second = ValidationPresets.Username();

        Assert.Equal(3, second.MinimumLength);
        Assert.Empty(second.AdditionalReserved);
        Assert.True(new Checker(second).IsClaimable("bluegarden"));
    }

    [Fact]
    public void DisplayNameIsExplicitlyLongerAndDoesNotChangeDefaultChecker()
    {
        var options = ValidationPresets.DisplayName();
        Assert.Equal(2, options.MinimumLength);
        Assert.Equal(64, options.MaximumLength);
        Assert.Equal(Rule.Whitespace, options.DisabledRules & Rule.Whitespace);
        Assert.Equal((Rule)0, new Options().DisabledRules & Rule.Whitespace);
        Assert.True(new Checker(options).IsClaimable("bluegarden"));
    }

    [Fact]
    public void OrganizationNameSupportsLongerIdentifiers()
    {
        var options = ValidationPresets.OrganizationName();
        Assert.Equal(2, options.MinimumLength);
        Assert.Equal(80, options.MaximumLength);
        Assert.Equal(Rule.Whitespace, options.DisabledRules & Rule.Whitespace);
        Assert.True(new Checker(options).IsClaimable("bluegarden"));
    }

    [Fact]
    public void GameTagHasExplicitAsciiAndLengthPolicy()
    {
        var options = ValidationPresets.GameTag();
        Assert.Equal(3, options.MinimumLength);
        Assert.Equal(24, options.MaximumLength);
        Assert.True(options.AsciiOnly);
        Assert.False(new Options().AsciiOnly);
        Assert.True(new Checker(options).IsClaimable("bluegarden"));
        Assert.True(new Checker(options).IsReserved("bluegardén"));
    }
}
