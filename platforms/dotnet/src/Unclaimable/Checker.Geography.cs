namespace Unclaimable;

internal static class GeographyData
{
    internal const string CountryReservationPrefix = "__unclaimable_internal_country_rule__:";
    internal const string CityReservationPrefix = "__unclaimable_internal_city_rule__:";

    internal static readonly string[] CountryNames =
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

    internal static readonly string[] PopularCityNames =
    {
        "abuja", "abudhabi", "accra", "adelaide", "addisababa", "algiers", "alexandria", "amsterdam", "ankara",
        "antwerp", "athens", "atlanta", "auckland", "austin", "baghdad", "baku", "baltimore", "bangalore",
        "bangkok", "barcelona", "beijing", "beirut", "belgrade", "bengaluru", "berlin", "birmingham", "bogota",
        "bogotá", "bologna", "bordeaux", "boston", "bratislava", "brisbane", "brussels", "bucharest", "budapest",
        "buenosaires", "busan", "cairo", "calgary", "capetown", "caracas", "casablanca", "cebu", "charlotte",
        "chennai", "chicago", "christchurch", "cleveland", "cologne", "colombo", "copenhagen", "dakar", "dallas",
        "dammam", "daressalaam", "delhi", "denver", "detroit", "dhaka", "doha", "dresden", "dubai", "dublin",
        "durban", "dusseldorf", "düsseldorf", "edinburgh", "edmonton", "florence", "frankfurt", "fukuoka", "geneva",
        "glasgow", "guadalajara", "guangzhou", "hamburg", "hanoi", "havana", "helsinki", "hochiminhcity", "hongkong",
        "honolulu", "houston", "hyderabad", "indianapolis", "istanbul", "jacksonville", "jakarta", "jeddah",
        "jerusalem", "johannesburg", "kansascity", "karachi", "kathmandu", "khartoum", "kigali", "kingston",
        "kolkata", "kualalumpur", "kyiv", "kyoto", "lagos", "lahore", "lasvegas", "lima", "lisbon", "liverpool",
        "ljubljana", "london", "losangeles", "luxembourgcity", "lyon", "macao", "macau", "madrid", "manchester",
        "manila", "marrakech", "marseille", "medellin", "medellín", "melbourne", "mexicocity", "miami", "milan",
        "milwaukee", "minneapolis", "montevideo", "montreal", "montréal", "moscow", "mumbai", "munich", "nairobi",
        "nanjing", "naples", "nashville", "newdelhi", "neworleans", "newyork", "newyorkcity", "nice", "osaka",
        "oslo", "ottawa", "orlando", "panamacity", "paris", "perth", "philadelphia", "phnompenh", "phoenix",
        "portland", "porto", "prague", "pretoria", "quebec", "quebeccity", "quito", "rabat", "reykjavik", "riga",
        "riodejaneiro", "riyadh", "rome", "rotterdam", "saintpetersburg", "salvador", "sandiego", "sanfrancisco",
        "sanjose", "santiago", "saopaulo", "sãopaulo", "seattle", "seoul", "seville", "shanghai", "shenzhen",
        "singapore", "sofia", "stockholm", "stuttgart", "sydney", "taipei", "tallinn", "tampa", "tashkent",
        "tbilisi", "tehran", "telaviv", "thehague", "thessaloniki", "tirana", "tokyo", "toronto", "toulouse",
        "tunis", "turin", "valencia", "vancouver", "venice", "vienna", "vilnius", "warsaw", "washington",
        "washingtondc", "wellington", "winnipeg", "wroclaw", "wrocław", "yangon", "yerevan", "zagreb", "zurich",
        "zürich"
    };
}
