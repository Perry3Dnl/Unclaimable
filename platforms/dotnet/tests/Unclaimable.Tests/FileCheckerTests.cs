using Unclaimable.Files;
using Xunit;

namespace Unclaimable.Tests;

public sealed class FileCheckerTests
{
    [Theory]
    [InlineData("annual_report_2026.pdf")]
    [InlineData("a")]
    [InlineData("123.txt")]
    [InlineData("aaa.txt")]
    [InlineData("REPORT.PDF")]
    [InlineData("my holiday photo.jpg")]
    [InlineData("amsterdam.txt")]
    [InlineData("admin.txt")]
    [InlineData(".gitignore")]
    [InlineData(".env.local")]
    [InlineData("résumé.pdf")]
    [InlineData("船🚢.png")]
    [InlineData("report..draft.pdf")]
    [InlineData("COM0.txt")]
    [InlineData("COM10.txt")]
    [InlineData("c0n.txt")]
    [InlineData("console.txt")]
    public void DefaultsAcceptOrdinaryFilenames(string name)
    {
        IFileChecker checker = new FileChecker();
        var result = checker.Check(name);
        Assert.True(result.IsAllowed);
        Assert.True(result.IsClaimable);
        Assert.True(checker.IsAllowed(name));
        Assert.True(checker.IsClaimable(name));
        Assert.Equal(FileFailureKind.None, result.FailureKind);
        Assert.Equal(name, result.FileName);
        Assert.Equal(name, result.CoreResult!.Input);
        Assert.False(result.CoreResult.IsReserved);
    }

    [Theory]
    [InlineData(null, FileFailureKind.MissingName)]
    [InlineData("", FileFailureKind.MissingName)]
    [InlineData("   ", FileFailureKind.MissingName)]
    [InlineData(".", FileFailureKind.DotSegment)]
    [InlineData("..", FileFailureKind.DotSegment)]
    [InlineData("../report.pdf", FileFailureKind.InvalidCharacter)]
    [InlineData("folder/report.pdf", FileFailureKind.InvalidCharacter)]
    [InlineData("folder\\report.pdf", FileFailureKind.InvalidCharacter)]
    [InlineData("C:report.pdf", FileFailureKind.InvalidCharacter)]
    [InlineData("a<b.txt", FileFailureKind.InvalidCharacter)]
    [InlineData("a>b.txt", FileFailureKind.InvalidCharacter)]
    [InlineData("a\"b.txt", FileFailureKind.InvalidCharacter)]
    [InlineData("a|b.txt", FileFailureKind.InvalidCharacter)]
    [InlineData("a?b.txt", FileFailureKind.InvalidCharacter)]
    [InlineData("a*b.txt", FileFailureKind.InvalidCharacter)]
    [InlineData("a\0b.txt", FileFailureKind.InvalidCharacter)]
    [InlineData("report.txt.", FileFailureKind.TrailingDotOrSpace)]
    [InlineData("report.txt ", FileFailureKind.TrailingDotOrSpace)]
    [InlineData("a\nb.txt", FileFailureKind.InvalidCharacter)]
    [InlineData("a\u202Eb.txt", FileFailureKind.NamePolicy)]
    [InlineData("\u200B", FileFailureKind.NamePolicy)]
    public void DefaultsRejectWithStructuredReasons(string? name, FileFailureKind expected)
    {
        var checker = new FileChecker();
        var result = checker.Check(name);
        Assert.False(result.IsAllowed);
        Assert.False(result.IsClaimable);
        Assert.False(checker.IsAllowed(name));
        Assert.False(checker.IsClaimable(name));
        Assert.Equal(name, result.FileName);
        Assert.Equal(expected, result.FailureKind);
    }

    public static IEnumerable<object[]> DeviceNames()
    {
        foreach (var name in new[] { "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$", "COM¹", "LPT²", "COM³" })
            yield return new object[] { name };
        for (var index = 1; index <= 9; index++)
        {
            yield return new object[] { "COM" + index };
            yield return new object[] { "LPT" + index };
        }
    }

    [Theory]
    [MemberData(nameof(DeviceNames))]
    public void DeviceReservationsUseCoreAndApplyBeforeEveryExtension(string device)
    {
        foreach (var suffix in new[] { "", ".txt", ".tar.gz", " .pdf" })
        {
            var result = new FileChecker().Check(device.ToLowerInvariant() + suffix);
            Assert.Equal(FileFailureKind.ReservedDeviceName, result.FailureKind);
            Assert.True(result.CoreResult!.IsReserved);
            Assert.Equal("file-device", result.CoreResult.Category);
            Assert.Equal(MatchKind.Exact, result.CoreResult.MatchKind);
        }
    }

    [Fact]
    public void ExplicitWindowsProfileDoesNotDependOnHostOrUtf8Length()
    {
        var checker = new FileChecker(new FileCheckerOptions { Profile = FileProfile.Windows });
        Assert.Equal(FileFailureKind.ReservedDeviceName, checker.Check("NUL.txt").FailureKind);
        Assert.Equal(FileFailureKind.TrailingDotOrSpace, checker.Check("a.").FailureKind);
        Assert.True(checker.IsAllowed(new string('船', 100)));
    }

    [Theory]
    [InlineData("CON.txt")]
    [InlineData("report.txt.")]
    [InlineData("report.txt ")]
    [InlineData("a:b?c*d<e>f|g\"h\\i")]
    public void LinuxPermitsItsOwnFilenameCharacters(string name)
    {
        Assert.True(new FileChecker(new FileCheckerOptions { Profile = FileProfile.Linux }).IsAllowed(name));
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("a\0b")]
    [InlineData("a\nb")]
    public void LinuxStillRejectsSeparatorsNulAndCoreUnicodeViolations(string name)
    {
        Assert.False(new FileChecker(new FileCheckerOptions { Profile = FileProfile.Linux }).IsAllowed(name));
    }

    [Fact]
    public void LengthLimitsIncludeTheExtensionAndCountUtf8Bytes()
    {
        var checker = new FileChecker(new FileCheckerOptions { MaximumLength = 8, MaximumUtf8Bytes = 8 });
        Assert.True(checker.IsAllowed("1234.txt"));
        Assert.Equal(FileFailureKind.TooLong, checker.Check("12345.txt").FailureKind);
        Assert.True(checker.IsAllowed("éééé"));
        Assert.Equal(FileFailureKind.TooLong, checker.Check("ééééé").FailureKind);
        Assert.True(checker.IsAllowed("🚢🚢"));
        Assert.Equal(FileFailureKind.TooLong, checker.Check("🚢🚢a").FailureKind);
    }

    [Fact]
    public void MalformedUtf16IsRejectedByCore()
    {
        var result = new FileChecker().Check("bad\uD800.txt");
        Assert.Equal(FileFailureKind.NamePolicy, result.FailureKind);
        Assert.Equal(MatchKind.InvalidCharacters, result.CoreResult!.MatchKind);
    }

    [Fact]
    public void AllowedExtensionsSupportCaseOptionalDotAndCompoundSuffixes()
    {
        var options = new FileCheckerOptions();
        options.AllowedExtensions.UnionWith(new[] { "PDF", ".tar.gz", ".pdf" });
        var checker = new FileChecker(options);
        Assert.True(checker.IsAllowed("report.PdF"));
        Assert.True(checker.IsAllowed("backup.TAR.GZ"));
        foreach (var name in new[] { "README", "report.pdf.exe", "backup.gz", ".pdf" })
            Assert.Equal(FileFailureKind.ExtensionNotAllowed, checker.Check(name).FailureKind);
    }

    [Fact]
    public void BlockedExtensionsWinOverAllowsAndCatchInnerExtensions()
    {
        var options = new FileCheckerOptions();
        options.AllowedExtensions.UnionWith(new[] { ".pdf", ".exe" });
        options.BlockedExtensions.UnionWith(new[] { "exe", ".tar.gz" });
        var checker = new FileChecker(options);
        foreach (var name in new[] { "run.EXE", "invoice.ExE.pdf", "backup.tar.gz", "backup.tar.gz.pdf" })
            Assert.Equal(FileFailureKind.BlockedExtension, checker.Check(name).FailureKind);
        Assert.True(checker.IsAllowed("execute.pdf"));
        Assert.True(checker.IsAllowed("example.exe_notes.pdf"));
        options.BlockInnerExtensions = false;
        var finalOnly = new FileChecker(options);
        Assert.True(finalOnly.IsAllowed("invoice.exe.pdf"));
        Assert.False(finalOnly.IsAllowed("run.exe"));
    }

    [Fact]
    public void NameReservationsUseCoreOnFullNamesAndStems()
    {
        var options = new FileCheckerOptions();
        options.NameOptions.Reserve("internal", "file-app", ReservedMatchMode.Exact);
        options.NameOptions.Reserve("secret.config", ReservedMatchMode.Exact);
        var checker = new FileChecker(options);
        var stem = checker.Check("INTERNAL.pdf");
        Assert.Equal(FileFailureKind.NamePolicy, stem.FailureKind);
        Assert.Equal("file-app", stem.CoreResult!.Category);
        Assert.Equal("INTERNAL", stem.CoreResult.Input);
        Assert.Equal(FileFailureKind.NamePolicy, checker.Check("secret.config").FailureKind);
        Assert.True(checker.IsAllowed("internal_notes.pdf"));
        Assert.True(checker.IsAllowed(".internal"));
    }

    [Fact]
    public void OptionalCoreProfanityAndObfuscationCanBeEnabled()
    {
        var options = new FileCheckerOptions();
        options.NameOptions.EnableCategory(Category.Profanity);
        options.NameOptions.EnableRule(Rule.Profanity | Rule.ObfuscationMatching | Rule.UnicodeConfusableMatching);
        var result = new FileChecker(options).Check("fuck.txt");
        Assert.Equal(FileFailureKind.NamePolicy, result.FailureKind);
        Assert.Equal("profanity", result.CoreResult!.Category);

        var custom = new FileCheckerOptions();
        custom.NameOptions.EnableRule(Rule.ObfuscationMatching);
        custom.NameOptions.Reserve("nike", ReservedMatchMode.WholeIdentifier);
        Assert.False(new FileChecker(custom).IsAllowed("n1ke.txt"));
    }

    [Fact]
    public void ApplicationAllowancesCannotBypassPlatformRules()
    {
        var options = new FileCheckerOptions();
        options.NameOptions.AllowedIdentifiers.Add("CON");
        options.NameOptions.AllowCharacters("/");
        options.NameOptions.RejectControlCharacters = false;
        var checker = new FileChecker(options);
        Assert.Equal(FileFailureKind.ReservedDeviceName, checker.Check("CON.txt").FailureKind);
        Assert.Equal(FileFailureKind.InvalidCharacter, checker.Check("a/b.txt").FailureKind);
        Assert.Equal(FileFailureKind.InvalidCharacter, checker.Check("a\u0001b.txt").FailureKind);
    }

    [Fact]
    public void ConstructionCapturesFileAndCoreSettings()
    {
        var options = new FileCheckerOptions();
        options.AllowedExtensions.Add("pdf");
        options.BlockedExtensions.Add("exe");
        var checker = new FileChecker(options);
        options.Profile = FileProfile.Linux;
        options.MaximumLength = 1;
        options.MaximumUtf8Bytes = 1;
        options.BlockInnerExtensions = false;
        options.AllowedExtensions.Clear();
        options.BlockedExtensions.Clear();
        options.NameOptions.Reserve("report", ReservedMatchMode.Exact);
        Assert.True(checker.IsAllowed("report.pdf"));
        Assert.False(checker.IsAllowed("report.txt"));
        Assert.Equal(FileFailureKind.BlockedExtension, checker.Check("run.exe.pdf").FailureKind);
        Assert.Equal(FileFailureKind.ReservedDeviceName, checker.Check("CON.pdf").FailureKind);
    }

    [Fact]
    public void CallerCanSupplyAnExistingCoreChecker()
    {
        var options = new FileCheckerOptions();
        options.NameOptions.Reserve("private", ReservedMatchMode.Exact);
        var checker = new FileChecker(new Checker(options.NameOptions), options);
        Assert.False(checker.IsAllowed("private.txt"));
        Assert.True(checker.IsAllowed("report.txt"));
    }

    [Fact]
    public void InjectedCoreRuntimePolicyRemainsLive()
    {
        var options = new FileCheckerOptions();
        options.NameOptions.EnableRule(Rule.BlockedCharacters);
        var policy = new Policy(options.NameOptions.ConfiguredBlockedCharacters);
        var checker = new FileChecker(new Checker(options.NameOptions, policy), options);
        Assert.True(checker.IsAllowed("readme"));
        policy.BlockCharacter("r");
        var result = checker.Check("readme");
        Assert.Equal(FileFailureKind.NamePolicy, result.FailureKind);
        Assert.Equal(MatchKind.BlockedCharacter, result.CoreResult!.MatchKind);
    }

    [Fact]
    public void InvalidConstructorArgumentsFailEarly()
    {
        var core = new Checker(new FileCheckerOptions().NameOptions);
        Assert.Throws<ArgumentNullException>(() => new FileChecker(null!));
        Assert.Throws<ArgumentNullException>(() => new FileChecker(null!, new FileCheckerOptions()));
        Assert.Throws<ArgumentNullException>(() => new FileChecker(core, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileChecker(new FileCheckerOptions { Profile = (FileProfile)42 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileChecker(new FileCheckerOptions { MaximumLength = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileChecker(new FileCheckerOptions { MaximumUtf8Bytes = 0 }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData("pdf.")]
    [InlineData("tar..gz")]
    [InlineData("p df")]
    [InlineData("p\ndf")]
    [InlineData("p/df")]
    [InlineData("p\\df")]
    [InlineData("p:df")]
    public void InvalidExtensionConfigurationIsRejected(string? extension)
    {
        var options = new FileCheckerOptions();
        options.AllowedExtensions.Add(extension!);
        Assert.Throws<ArgumentException>(() => new FileChecker(options));
        options.AllowedExtensions.Clear();
        options.BlockedExtensions.Add(extension!);
        Assert.Throws<ArgumentException>(() => new FileChecker(options));
    }

    [Fact]
    public void SurrogateExtensionConfigurationIsRejected()
    {
        var options = new FileCheckerOptions();
        options.AllowedExtensions.Add("p\uD800df");
        Assert.Throws<ArgumentException>(() => new FileChecker(options));
    }
}
