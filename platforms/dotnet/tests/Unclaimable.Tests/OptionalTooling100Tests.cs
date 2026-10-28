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
    [Fact]
    public void InspectorCoversAllInvalidNumericBoundariesWithoutMutation()
    {
        var options = new Options
        {
            MinimumLength = -1,
            MaximumLength = 0,
            PartialMatchMinimumLength = 0,
            RepeatedPatternMinimumLength = 1
        };

        var problems = PolicyConfigurationInspector.Inspect(options);
        Assert.Contains(problems, p => p.Contains("MinimumLength must not be negative"));
        Assert.Contains(problems, p => p.Contains("MaximumLength must be at least 1"));
        Assert.Contains(problems, p => p.Contains("PartialMatchMinimumLength must be at least 1"));
        Assert.Contains(problems, p => p.Contains("RepeatedPatternMinimumLength must be at least 2"));
        Assert.Equal(4, problems.Count);
        Assert.Equal(-1, options.MinimumLength);
        Assert.Equal(0, options.MaximumLength);
        Assert.Equal(0, options.PartialMatchMinimumLength);
        Assert.Equal(1, options.RepeatedPatternMinimumLength);
    }

    [Fact]
    public void InspectorReportsUnsupportedObfuscationSensitivity()
    {
        var options = new Options
        {
            ObfuscationSensitivity = (ObfuscationSensitivity)999
        };

        var problems = PolicyConfigurationInspector.Inspect(options);
        Assert.Single(problems);
        Assert.Contains("ObfuscationSensitivity", problems[0]);
        Assert.Equal((ObfuscationSensitivity)999, options.ObfuscationSensitivity);
        Assert.Throws<ArgumentOutOfRangeException>(() => new Checker(options));
    }

    [Fact]
    public void InspectorAcceptsValidConfigurationBoundaries()
    {
        var options = new Options
        {
            MinimumLength = 0,
            MaximumLength = 1,
            PartialMatchMinimumLength = 1,
            RepeatedPatternMinimumLength = 2
        };

        Assert.Empty(PolicyConfigurationInspector.Inspect(options));
    }

    [Fact]
    public void ExplanationsDescribeCategoryAndOriginalOffsetsWithoutChangingResults()
    {
        var diagnostic = new Diagnostic(
            MatchKind.Partial,
            matchedValue: "zorb",
            category: "custom",
            matchStartIndex: 2,
            matchLength: 4);
        var description = ValidationExplanation.Describe(diagnostic);

        Assert.Contains("Partial", description);
        Assert.Contains("custom", description);
    }

    [Fact]
    public void ExplanationsReportOriginalOffendingCharacterIndex()
    {
        var diagnostic = new Diagnostic(
            MatchKind.BlockedCharacter,
            offendingCharacterIndex: 2,
            offendingCharacter: "/");
        Assert.Contains("original UTF-16 offset 2", ValidationExplanation.Describe(diagnostic));
    }

}
