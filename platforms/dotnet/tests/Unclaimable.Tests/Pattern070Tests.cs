using Xunit;

namespace Unclaimable.Tests;

public sealed class Pattern070Tests
{
    [Fact]
    public void DefaultPatternsEnableObjectiveShapeChecksButLeaveUppercaseOptIn()
    {
        var options = new Options();

        Assert.True((options.EnabledPatterns & Pattern.NumericOnly) != 0);
        Assert.True((options.EnabledPatterns & Pattern.Repeated) != 0);
        Assert.True((options.EnabledPatterns & Pattern.SymbolOnly) != 0);
        Assert.True((options.EnabledPatterns & Pattern.AsciiArt) != 0);
        Assert.False((options.EnabledPatterns & Pattern.UppercaseOnly) != 0);
        Assert.Equal(4, options.RepeatedPatternMinimumLength);
    }

    [Theory]
    [InlineData("39742397429374")]
    [InlineData("123456789")]
    [InlineData("٠١٢٣٤٥٦٧٨٩")]
    [InlineData("１２３４５６７８９")]
    public void NumericOnlyRejectsWholeDigitIdentifiersWhenNumbersAreOtherwiseAllowed(string value)
    {
        var checker = new Checker(new Options
        {
            AllowNumbers = true
        });

        var result = checker.Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.NumericOnly, result.MatchKind);
    }

    [Fact]
    public void DisablingNumericOnlyDoesNotClearARepeatedNumericIdentifier()
    {
        var options = new Options
        {
            AllowNumbers = true
        };
        options.DisablePattern(Pattern.NumericOnly);

        var result = new Checker(options).Check("111111111111");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.RepeatedPattern, result.MatchKind);
    }

    [Fact]
    public void DisablingNumericOnlyAllowsANonRepeatedNumericIdentifierToContinue()
    {
        var options = new Options
        {
            AllowNumbers = true
        };
        options.DisablePattern(Pattern.NumericOnly);

        Assert.True(new Checker(options).Check("39742397429374").IsClaimable);
    }

    [Theory]
    [InlineData("dddd")]
    [InlineData("aaaaaaaaaaaaaaaa")]
    [InlineData("asasas")]
    [InlineData("asasasasasa")]
    [InlineData("asasasasasas")]
    [InlineData("abababababababab")]
    [InlineData("abcabcabcabc")]
    [InlineData("AaAaAaAaAaAa")]
    [InlineData("hahaha")]
    [InlineData("sssssssss2234423")]
    [InlineData("useraaaa12")]
    [InlineData("testabababab99")]
    public void RepeatedRejectsRepeatedSpansAtOrAboveTheConfiguredMinimum(string value)
    {
        var result = new Checker().Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.RepeatedPattern, result.MatchKind);
    }

    [Theory]
    [InlineData("aaa")]
    [InlineData("asas")]
    [InlineData("zabcabc9")]
    [InlineData("rememberme")]
    [InlineData("bookkeeper")]
    [InlineData("Hannah")]
    public void RepeatedDoesNotRejectSpansBelowTheDefaultMinimum(string value)
    {
        Assert.True(new Checker().Check(value).IsClaimable);
    }

    [Fact]
    public void RepeatedPatternMinimumLengthCanBeRaised()
    {
        var options = new Options
        {
            RepeatedPatternMinimumLength = 8
        };
        var checker = new Checker(options);

        Assert.True(checker.Check("sssssss2234423").IsClaimable);

        var result = checker.Check("ssssssss2234423");
        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.RepeatedPattern, result.MatchKind);
    }

    [Fact]
    public void RepeatedPatternMinimumLengthIsCapturedWhenCheckerIsConstructed()
    {
        var options = new Options
        {
            RepeatedPatternMinimumLength = 8
        };
        var captured = new Checker(options);

        options.RepeatedPatternMinimumLength = 4;

        Assert.True(captured.Check("ssss2234423").IsClaimable);
        Assert.Equal(
            MatchKind.RepeatedPattern,
            new Checker(options).Check("ssss2234423").MatchKind);
    }

    [Fact]
    public void RepeatedPatternMinimumLengthOfTwoIsSupported()
    {
        var options = new Options
        {
            RepeatedPatternMinimumLength = 2
        };

        var result = new Checker(options).Check("aabc");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.RepeatedPattern, result.MatchKind);
    }

    [Fact]
    public void RepeatedPatternMinimumLengthMustBeAtLeastTwo()
    {
        var options = new Options
        {
            RepeatedPatternMinimumLength = 1
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => new Checker(options));
    }

    [Fact]
    public void DisablingRepeatedLetsRepeatedTextContinue()
    {
        var options = new Options();
        options.DisablePattern(Pattern.Repeated);

        Assert.True(new Checker(options).Check("aaaaaaaaaaaaaaaa").IsClaimable);
    }

    [Theory]
    [InlineData("========")]
    [InlineData("!@#$")]
    [InlineData("()[]{}")]
    public void SymbolOnlyRejectsIdentifiersWithoutLettersOrNumbers(string value)
    {
        var result = new Checker().Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.SymbolOnly, result.MatchKind);
    }

    [Fact]
    public void DisablingSymbolOnlyLetsNonRepeatedSymbolsContinue()
    {
        var options = new Options();
        options.DisablePattern(Pattern.SymbolOnly);

        Assert.True(new Checker(options).Check("!@#$").IsClaimable);
    }

    [Theory]
    [InlineData("8===3")]
    [InlineData("8====D")]
    [InlineData("B---D")]
    public void AsciiArtRejectsCuratedShapePatterns(string value)
    {
        var options = new Options
        {
            AllowNumbers = true,
            DisabledRules = Rule.BlockedCharacters | Rule.LeadingSeparator | Rule.TrailingSeparator
        };

        var result = new Checker(options).Check(value);

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.AsciiArt, result.MatchKind);
    }

    [Fact]
    public void DisablingAsciiArtLetsTheShapeContinueThroughOtherEnabledChecks()
    {
        var options = new Options
        {
            AllowNumbers = true
        };
        options.DisablePattern(Pattern.AsciiArt);

        Assert.True(new Checker(options).Check("8===3").IsClaimable);
    }

    [Fact]
    public void UppercaseOnlyIsOptIn()
    {
        var defaultChecker = new Checker();
        Assert.True(defaultChecker.Check("QZXVORN").IsClaimable);

        var options = new Options();
        options.EnablePattern(Pattern.UppercaseOnly);

        var result = new Checker(options).Check("QZXVORN");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.UppercaseOnly, result.MatchKind);
    }

    [Theory]
    [InlineData("Qzxvorn")]
    [InlineData("qzxvorn")]
    public void UppercaseOnlyDoesNotRejectMixedOrLowercaseIdentifiers(string value)
    {
        var options = new Options();
        options.EnablePattern(Pattern.UppercaseOnly);

        Assert.True(new Checker(options).Check(value).IsClaimable);
    }

    [Fact]
    public void AllowingUppercaseOnlyDoesNotAllowAReservedUppercaseIdentifier()
    {
        var options = new Options();
        options.DisablePattern(Pattern.UppercaseOnly);

        var result = new Checker(options).Check("ADMIN");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.Exact, result.MatchKind);
        Assert.Equal("admin", result.MatchedValue);
    }

    [Fact]
    public void DetailedCheckCanReportPatternAndReservedReasonsTogether()
    {
        var options = new Options();
        options.EnablePattern(Pattern.UppercaseOnly);

        var result = new Checker(options).CheckDetailed("ADMIN", includeMessages: true);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Kind == MatchKind.UppercaseOnly);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Kind == MatchKind.Exact
            && string.Equals(diagnostic.MatchedValue, "admin", StringComparison.Ordinal));
    }

    [Fact]
    public void AllowedIdentifierDoesNotBypassPatternChecks()
    {
        var options = new Options();
        options.EnablePattern(Pattern.UppercaseOnly);
        options.AllowedIdentifiers.Add("qzxvorn");

        var result = new Checker(options).Check("QZXVORN");

        Assert.True(result.IsReserved);
        Assert.Equal(MatchKind.UppercaseOnly, result.MatchKind);
    }

    [Fact]
    public void PatternConfigurationIsCapturedWhenCheckerIsConstructed()
    {
        var options = new Options();
        var captured = new Checker(options);

        options.EnablePattern(Pattern.UppercaseOnly);

        Assert.True(captured.Check("QZXVORN").IsClaimable);
        Assert.Equal(MatchKind.UppercaseOnly, new Checker(options).Check("QZXVORN").MatchKind);
    }

    [Fact]
    public void PatternMethodsAcceptCombinationsAndRejectUnknownBits()
    {
        var options = new Options();

        options.DisablePattern(Pattern.NumericOnly | Pattern.Repeated);
        Assert.False((options.EnabledPatterns & Pattern.NumericOnly) != 0);
        Assert.False((options.EnabledPatterns & Pattern.Repeated) != 0);

        options.EnablePattern(Pattern.NumericOnly | Pattern.Repeated);
        Assert.True((options.EnabledPatterns & Pattern.NumericOnly) != 0);
        Assert.True((options.EnabledPatterns & Pattern.Repeated) != 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => options.EnablePattern((Pattern)(1 << 20)));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DisablePattern((Pattern)(1 << 20)));
    }

    [Fact]
    public void ActualNullInputKeepsExistingClaimableBehavior()
    {
        Assert.True(new Checker().IsClaimable(null));
    }
}
