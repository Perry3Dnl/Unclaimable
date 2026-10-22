using System;
using Xunit;

namespace Unclaimable.Tests;

public sealed class OptionalTooling100Tests
{
    [Fact]
    public void DefaultOptionsHaveNoConfigurationProblems()
    {
        var options = new Options();
        Assert.Empty(PolicyConfigurationInspector.Inspect(options));
        Assert.Equal(3, options.MinimumLength);
        Assert.Equal(32, options.MaximumLength);
    }

    [Fact]
    public void InspectorReportsInvalidLengthsWithoutMutatingOptions()
    {
        var options = new Options { MinimumLength = 12, MaximumLength = 5 };
        var problems = PolicyConfigurationInspector.Inspect(options);

        Assert.Contains(problems, message => message.Contains("exceed"));
        Assert.Equal(12, options.MinimumLength);
        Assert.Equal(5, options.MaximumLength);
        Assert.Throws<ArgumentException>(() => new Checker(options));
    }

    [Fact]
    public void InspectorReportsInvalidSensitivityWithoutChangingLegacyConstructorException()
    {
        var options = new Options { FalsePositiveSensitivity = (FalsePositiveSensitivity)999 };
        Assert.Contains(PolicyConfigurationInspector.Inspect(options), x => x.Contains("FalsePositiveSensitivity"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Checker(options));
    }

    [Fact]
    public void ExplanationsDoNotChangeMatchingDecisions()
    {
        var checker = new Checker();
        var detailed = checker.CheckDetailed("saddick");
        Assert.True(detailed.IsReserved);

        var explanations = ValidationExplanation.Describe(detailed);
        Assert.Equal(detailed.Diagnostics.Count, explanations.Count);
        Assert.Contains("Partial", explanations[0]);
        Assert.False(checker.IsClaimable("saddick"));
    }

    [Fact]
    public void AllowedChecksYieldNoExplanations()
    {
        var result = new Checker().CheckDetailed("bluegarden");
        Assert.True(result.IsClaimable);
        Assert.Empty(ValidationExplanation.Describe(result));
    }

    [Fact]
    public void HelpersRejectNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PolicyConfigurationInspector.Inspect(null!));
        Assert.Throws<ArgumentNullException>(() => ValidationExplanation.Describe((Diagnostic)null!));
        Assert.Throws<ArgumentNullException>(() => ValidationExplanation.Describe((DetailedResult)null!));
    }
}
