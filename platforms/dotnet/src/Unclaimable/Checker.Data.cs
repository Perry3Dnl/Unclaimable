using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Unclaimable;

public sealed partial class Checker
{
    private static string? NormalizeExact(string? value)
    {
        if (value is null || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().Normalize(NormalizationForm.FormKC).ToLowerInvariant();
    }

    private static string NormalizeCompact(string value)
    {
        var builder = new StringBuilder(value.Length);

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];

            if (char.IsHighSurrogate(character)
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
                if (IsLetterOrDigit(category))
                {
                    builder.Append(character);
                    builder.Append(value[index + 1]);
                }

                index++;
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static bool IsLetterOrDigit(UnicodeCategory category)
    {
        return category == UnicodeCategory.UppercaseLetter
               || category == UnicodeCategory.LowercaseLetter
               || category == UnicodeCategory.TitlecaseLetter
               || category == UnicodeCategory.ModifierLetter
               || category == UnicodeCategory.OtherLetter
               || category == UnicodeCategory.DecimalDigitNumber;
    }

    private static IReadOnlyList<ReservedEntry> LoadBuiltInEntries()
    {
        var assembly = typeof(Checker).Assembly;
        var entries = new List<ReservedEntry>();
        var serializer = new DataContractJsonSerializer(typeof(ReservedListDocument));

        foreach (var resourceName in assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith("Unclaimable.Data.", StringComparison.Ordinal)
                                    && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream is null)
                {
                    throw new InvalidOperationException($"Embedded dataset '{resourceName}' could not be opened.");
                }

                var document = serializer.ReadObject(stream) as ReservedListDocument;
                if (document is null)
                {
                    throw new InvalidOperationException($"Embedded dataset '{resourceName}' is invalid.");
                }

                if ((document.Schema != 1 && document.Schema != 2) || string.IsNullOrWhiteSpace(document.Category))
                {
                    throw new InvalidOperationException($"Embedded dataset '{resourceName}' has an unsupported schema.");
                }

                var language = ResolveDatasetLanguage(document, resourceName);

                entries.AddRange((document.Values ?? Array.Empty<string>())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => new ReservedEntry(value, document.Category, language)));

                if (document.Schema >= 2)
                {
                    entries.AddRange((document.PartialValues ?? Array.Empty<string>())
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => new ReservedEntry(value, document.Category, language, safePartial: true)));

                    foreach (var combination in document.Combinations ?? Array.Empty<CombinationDocument>())
                    {
                        if (combination is null)
                        {
                            continue;
                        }

                        foreach (var root in (combination.Roots ?? Array.Empty<string>())
                                     .Where(root => !string.IsNullOrWhiteSpace(root)))
                        {
                            foreach (var suffix in (combination.Suffixes ?? Array.Empty<string>())
                                         .Where(suffix => !string.IsNullOrWhiteSpace(suffix)))
                            {
                                entries.Add(new ReservedEntry(
                                    root + suffix,
                                    document.Category,
                                    language,
                                    safePartial: combination.Partial));
                            }
                        }
                    }
                }
            }
        }

        return entries;
    }

    private static Language? ResolveDatasetLanguage(ReservedListDocument document, string resourceName)
    {
        var language = document.Language?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(language))
        {
            if (string.Equals(document.Category, "brands", StringComparison.Ordinal)
                || string.Equals(document.Category, "technology", StringComparison.Ordinal))
            {
                return null;
            }

            return Language.English;
        }

        switch (language)
        {
            case "global":
                return null;
            case "en":
            case "eng":
            case "english":
                return Language.English;
            case "nl":
            case "nld":
            case "dutch":
                return Language.Dutch;
            case "de":
            case "deu":
            case "ger":
            case "german":
            case "deutsch":
                return Language.German;
            case "fr":
            case "fra":
            case "fre":
            case "french":
            case "francais":
                return Language.French;
            case "es":
            case "spa":
            case "spanish":
            case "espanol":
                return Language.Spanish;
            case "it":
            case "ita":
            case "italian":
            case "italiano":
                return Language.Italian;
            case "pt":
            case "por":
            case "portuguese":
            case "portugues":
                return Language.Portuguese;
            case "pl":
            case "pol":
            case "polish":
                return Language.Polish;
            case "tr":
            case "tur":
            case "turkish":
                return Language.Turkish;
            case "id":
            case "ind":
            case "indonesian":
                return Language.Indonesian;
            case "cs":
            case "ces":
            case "cze":
            case "czech":
                return Language.Czech;
            case "vi":
            case "vie":
            case "vietnamese":
                return Language.Vietnamese;
            case "hu":
            case "hun":
            case "hungarian":
                return Language.Hungarian;
            case "sv":
            case "swe":
            case "swedish":
                return Language.Swedish;
            case "ro":
            case "ron":
            case "rum":
            case "romanian":
                return Language.Romanian;
            default:
                throw new InvalidOperationException(
                    $"Embedded dataset '{resourceName}' declares unsupported language '{language}'.");
        }
    }
}
