using System;
using Unclaimable;
using Unclaimable.Email;
using Xunit;

namespace Unclaimable.Tests;

public sealed class DefensiveApi084Tests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("@")]
    [InlineData("a@")]
    [InlineData("@example.com")]
    [InlineData("a@@example.com")]
    [InlineData(" a@example.com")]
    [InlineData("a@example.com ")]
    [InlineData("a@example..com")]
    [InlineData("a@.example.com")]
    [InlineData("a@example.com.")]
    [InlineData("a@-example.com")]
    [InlineData("a@example-.com")]
    [InlineData("a@xn--.com")]
    public void MalformedMailboxInputsReturnResultsWithoutThrowing(string? candidate)
    {
        var options = EmailOptions.ForUserRegistration();
        options.ProtectedDomains.Add("example.co.za");
        var checker = new EmailChecker(options);
        var result = checker.CheckExistingAddress(candidate);
        Assert.NotNull(result);
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void InvalidUtf16CodeUnitsAreRejectedWithoutThrowing()
    {
        var checker = new EmailChecker(EmailOptions.ForUserRegistration());
        var malformed = new[]
        {
            new string(new[] { (char)0xD800, '@', 'e', 'x', 'a', 'm', 'p', 'l', 'e', '.', 'c', 'o', 'm' }),
            new string(new[] { (char)0xDC00, '@', 'e', 'x', 'a', 'm', 'p', 'l', 'e', '.', 'c', 'o', 'm' }),
            "a@ex" + new string(new[] { (char)0xD800 }) + "ample.com"
        };
        foreach (var address in malformed)
        {
            var result = checker.CheckExistingAddress(address);
            Assert.False(result.IsAllowed);
        }
    }

    [Fact]
    public void DeterministicMalformedInputFuzzNeverCrashesEmailOrCore()
    {
        var options = EmailOptions.ForUserRegistration();
        options.ProtectedDomains.Add("example.co.za");
        options.IssuingDomains.Add("example.com");
        var email = new EmailChecker(options);
        var core = new Checker();
        var random = new Random(804);
        var alphabet = new[] { 'a', 'Z', '0', '.', '@', '-', '_', '\uD800', '\uDC00', '\u200D', '\u0301', '\u03BF', '\u0430', ' ', '\0' };

        for (var sample = 0; sample < 400; sample++)
        {
            var length = random.Next(0, 160);
            var chars = new char[length];
            for (var index = 0; index < length; index++)
                chars[index] = alphabet[random.Next(alphabet.Length)];
            var candidate = new string(chars);

            Assert.NotNull(core.Check(candidate));
            Assert.NotNull(email.CheckExistingAddress(candidate));
            Assert.NotNull(email.CheckNewAddress(candidate));
        }
    }

    [Fact]
    public void ConfigurationSnapshotDoesNotChangeWhenOptionsMutate()
    {
        var options = new EmailOptions { AllowIssuingDomainSubdomains = false };
        options.IssuingDomains.Add("example.com");
        var checker = new EmailChecker(options);
        options.IssuingDomains.Clear();
        options.AllowIssuingDomainSubdomains = true;
        Assert.False(checker.CheckNewAddress("bluegarden@sub.example.com").IsAllowed);
        Assert.True(checker.CheckNewAddress("bluegarden@example.com").IsAllowed);
    }

    [Theory]
    [InlineData("bluegarden@example.com")]
    [InlineData("blue.garden+tag@example.com")]
    [InlineData("bluegarden@sub.example.com")]
    [InlineData("bluegarden@xn--bcher-kva.de")]
    [InlineData("bluegarden@bücher.de")]
    public void NormalMailboxHappyFlowsStayFunctional(string value)
    {
        var result = new EmailChecker(EmailOptions.ForUserRegistration()).CheckExistingAddress(value);
        Assert.True(result.IsAllowed);
        Assert.True(result.IsSyntaxValid);
        Assert.True(result.IsIssuingDomainAllowed);
    }
}
