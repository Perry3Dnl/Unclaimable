using System;
using System.Collections.Generic;
using System.Text;
using Unclaimable;
using Unclaimable.Extended;

// Identical source is compiled against NuGet 0.8.5 and 1.0.0 project outputs.
// This is a deterministic, privacy-safe *decision* corpus, not a performance benchmark.
var extended = args.Length == 1 && args[0] == "--extended";
var options = new Options();
if (extended)
{
    options.UseExtendedData();
}

var checker = new Checker(options);
var seeds = new string?[]
{
    null, "", "ab", "bluegarden", "ordinary2", "admin", "customersupport",
    "saddick", "dickinson", "dickgarden", "piemel", "godverdomme",
    "cocktail", "supportive", "mycommissarisvandekoningname",
    "commissarisvandekoning", "123456789", "ordinary_user", "ordinary-user",
    "myloginverificationteamx", "p3n1s", "myp3n1sname", "zzzzzz",
    "asasasasasa", "aaaaaaaaab", "bluegardén", "船🚢", "user＠name"
};

var corpus = new List<string?>(seeds);
var random = new Random(120100);
const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
for (var i = 0; i < 1400; i++)
{
    var length = random.Next(2, 42);
    var builder = new StringBuilder(length);
    for (var j = 0; j < length; j++)
    {
        builder.Append(alphabet[random.Next(alphabet.Length)]);
    }

    corpus.Add(builder.ToString());
}

var fixedTerms = new[] { "admin", "support", "dick", "penis", "zorb", "commissarisvandekoning" };
foreach (var term in fixedTerms)
{
    corpus.Add(term);
    corpus.Add("bluegarden" + term);
    corpus.Add(term + "bluegarden");
    corpus.Add("bluegarden" + term + "bluegarden");
}

foreach (var value in corpus)
{
    var result = checker.Check(value);
    var detailed = checker.CheckDetailed(value);
    var serializedInput = value is null ? "<null>" : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    Console.WriteLine(string.Join("\t",
        serializedInput,
        checker.IsClaimable(value),
        checker.IsReserved(value),
        result.IsClaimable,
        result.MatchKind,
        result.Category ?? "",
        result.MatchedValue ?? "",
        detailed.IsReserved));
}
