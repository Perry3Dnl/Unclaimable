namespace Unclaimable;

public sealed partial class Checker
{
    private static readonly HashSet<string> CountryNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "afghanistan", "albania", "algeria", "andorra", "angola", "antiguaandbarbuda", "argentina",
        "armenia", "australia", "austria", "azerbaijan", "bahamas", "bahrain", "bangladesh", "barbados",
        "belarus", "belgium", "belize", "benin", "bhutan", "bolivia", "bosniaandherzegovina", "botswana",
        "brazil", "brunei", "bulgaria", "burkinafaso", "burundi", "caboverde", "capeverde", "cambodia",
        "cameroon", "canada", "centralafricanrepublic", "chad", "chile", "china", "colombia", "comoros",
        "congo", "costarica", "cotedivoire", "côtedivoire", "ivorycoast", "croatia", "cuba", "cyprus",
        "czechia", "czechrepublic", "democraticrepublicofthecongo", "drc", "drcongo", "denmark", "djibouti",
        "dominica", "dominicanrepublic", "easttimor", "ecuador", "egypt", "elsalvador", "equatorialguinea",
        "eritrea", "estonia", "eswatini", "swaziland", "ethiopia", "fiji", "finland", "france", "gabon",
        "gambia", "georgia", "germany", "ghana", "greece", "grenada", "guatemala", "guinea", "guineabissau",
        "guyana", "haiti", "honduras", "hungary", "iceland", "india", "indonesia", "iran", "iraq", "ireland",
        "israel", "italy", "jamaica", "japan", "jordan", "kazakhstan", "kenya", "kiribati", "kosovo", "kuwait",
        "kyrgyzstan", "laos", "latvia", "lebanon", "lesotho", "liberia", "libya", "liechtenstein", "lithuania",
        "luxembourg", "madagascar", "malawi", "malaysia", "maldives", "mali", "malta", "marshallislands",
        "mauritania", "mauritius", "mexico", "micronesia", "moldova", "monaco", "mongolia", "montenegro",
        "morocco", "mozambique", "myanmar", "burma", "namibia", "nauru", "nepal", "netherlands", "newzealand",
        "nicaragua", "niger", "nigeria", "northkorea", "northmacedonia", "norway", "oman", "pakistan", "palau",
        "palestine", "panama", "papuanewguinea", "paraguay", "peru", "philippines", "poland", "portugal",
        "qatar", "republicofthecongo", "romania", "russia", "russianfederation", "rwanda", "saintkittsandnevis",
        "saintlucia", "saintvincentandthegrenadines", "samoa", "sanmarino", "saotomeandprincipe",
        "sãotoméandpríncipe", "saudiarabia", "senegal", "serbia", "seychelles", "sierraleone", "singapore",
        "slovakia", "slovenia", "solomonislands", "somalia", "southafrica", "southkorea", "southsudan", "spain",
        "srilanka", "sudan", "suriname", "sweden", "switzerland", "syria", "taiwan", "tajikistan", "tanzania",
        "thailand", "timorleste", "togo", "tonga", "trinidadandtobago", "tunisia", "turkey", "turkiye", "türkiye",
        "turkmenistan", "tuvalu", "uganda", "ukraine", "unitedarabemirates", "uae", "unitedkingdom", "uk",
        "unitedstates", "unitedstatesofamerica", "usa", "america", "uruguay", "uzbekistan", "vanuatu", "vatican",
        "vaticancity", "venezuela", "vietnam", "yemen", "zambia", "zimbabwe"
    };

    private static readonly HashSet<string> PopularCityNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "abuja", "abudhabi", "accra", "adelaide", "addisababa", "algiers", "alexandria", "amsterdam", "ankara",
        "antwerp", "athens", "atlanta", "auckland", "austin", "baghdad", "baku", "baltimore", "bangalore",
        "bangkok", "barcelona", "beijing", "beirut", "belgrade", "bengaluru", "berlin", "birmingham", "bogota",
        "bogotá", "bologna", "bordeaux", "boston", "bratislava", "brisbane", "brussels", "bucharest", "budapest",
        "buenosaires", "busan", "cairo", "calgary", "capetown", "caracas", "casablanca", "cebu", "charlotte",
        "chennai", "chicago", "christchurch", "cleveland", "cologne", "colombo", "copenhagen", "dakar", "dallas",
        "dammam", "darel salaam", "daressalaam", "delhi", "denver", "detroit", "dhaka", "doha", "dresden",
        "dubai", "dublin", "durban", "dusseldorf", "düsseldorf", "edinburgh", "edmonton", "florence", "frankfurt",
        "fukuoka", "geneva", "glasgow", "guadalajara", "guangzhou", "hamburg", "hanoi", "havana", "helsinki",
        "hochiminhcity", "hongkong", "honolulu", "houston", "hyderabad", "indianapolis", "istanbul", "jacksonville",
        "jakarta", "jeddah", "jerusalem", "johannesburg", "kansascity", "karachi", "kathmandu", "khartoum",
        "kigali", "kingston", "kolkata", "kualalumpur", "kyiv", "kyoto", "lagos", "lahore", "lasvegas",
        "lima", "lisbon", "liverpool", "ljubljana", "london", "losangeles", "luxembourgcity", "lyon", "macao",
        "macau", "madrid", "manchester", "manila", "marrakech", "marseille", "medellin", "medellín", "melbourne",
        "mexicocity", "miami", "milan", "milwaukee", "minneapolis", "montevideo", "montreal", "montréal", "moscow",
        "mumbai", "munich", "nairobi", "nanjing", "naples", "nashville", "newdelhi", "neworleans", "newyork",
        "newyorkcity", "nice", "osaka", "oslo", "ottawa", "orlando", "panamacity", "paris", "perth", "philadelphia",
        "phnompenh", "phoenix", "portland", "porto", "prague", "pretoria", "quebec", "quebeccity", "quito", "rabat",
        "reykjavik", "riga", "riodejaneiro", "riyadh", "rome", "rotterdam", "saintpetersburg", "salvador", "sandiego",
        "sanfrancisco", "sanjose", "santiago", "saopaulo", "sãopaulo", "seattle", "seoul", "seville", "shanghai",
        "shenzhen", "singapore", "sofia", "stockholm", "stuttgart", "sydney", "taipei", "tallinn", "tampa", "tashkent",
        "tbilisi", "tehran", "telaviv", "thehague", "thessaloniki", "tirana", "tokyo", "toronto", "toulouse", "tunis",
        "turin", "valencia", "vancouver", "venice", "vienna", "vilnius", "warsaw", "washington", "washingtondc",
        "wellington", "winnipeg", "wroclaw", "wrocław", "yangon", "yerevan", "zagreb", "zurich", "zürich"
    };

    private bool TryFindFirstGeographyViolation(string? value, out Result? violation)
    {
        violation = null;

        if (!_countryNamesEnabled && !_popularCityNamesEnabled)
        {
            return false;
        }

        var normalized = NormalizeGeographyName(value);
        if (normalized is null)
        {
            return false;
        }

        if (_countryNamesEnabled && CountryNames.Contains(normalized))
        {
            violation = new Result(true, value, normalized, null, MatchKind.CountryName);
            return true;
        }

        if (_popularCityNamesEnabled && PopularCityNames.Contains(normalized))
        {
            violation = new Result(true, value, normalized, null, MatchKind.PopularCityName);
            return true;
        }

        return false;
    }

    private void CollectGeographyDiagnostics(string? value, bool includeMessages, List<Diagnostic> diagnostics)
    {
        if (!_countryNamesEnabled && !_popularCityNamesEnabled)
        {
            return;
        }

        var normalized = NormalizeGeographyName(value);
        if (normalized is null)
        {
            return;
        }

        if (_countryNamesEnabled && CountryNames.Contains(normalized))
        {
            diagnostics.Add(ToDiagnostic(
                new Result(true, value, normalized, null, MatchKind.CountryName),
                includeMessages));
        }

        if (_popularCityNamesEnabled && PopularCityNames.Contains(normalized))
        {
            diagnostics.Add(ToDiagnostic(
                new Result(true, value, normalized, null, MatchKind.PopularCityName),
                includeMessages));
        }
    }

    private static string? NormalizeGeographyName(string? value)
    {
        var exact = NormalizeExact(value);
        if (exact is null)
        {
            return null;
        }

        var compact = NormalizeCompact(exact);
        return compact.Length == 0 ? null : compact;
    }
}
