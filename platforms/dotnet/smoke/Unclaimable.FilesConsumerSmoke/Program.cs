using Unclaimable;
using Unclaimable.Files;

// This project deliberately references only the Files package. Core must arrive transitively.
var options = new FileCheckerOptions();
options.NameOptions.Reserve("internal", ReservedMatchMode.Exact);
var checker = new FileChecker(options);
if (!checker.IsAllowed("annual_report_2026.pdf")
    || checker.Check("CON.txt").CoreResult?.Category != "file-device"
    || checker.Check("internal.pdf").CoreResult?.MatchedValue != "internal"
    || typeof(Checker).Assembly.GetName().Name != "Unclaimable")
    throw new InvalidOperationException("Files package must resolve and use Unclaimable Core transitively.");

Console.WriteLine("Files-only packaged consumer passed with transitive Core dependency.");
