namespace Unclaimable;

internal static class OptionalIdentityData
{
    internal const string NationalityPrefix = "__unclaimable_internal_nationality_rule__:";
    internal const string CurrencyPrefix = "__unclaimable_internal_currency_rule__:";
    internal const string ReligionPrefix = "__unclaimable_internal_religion_rule__:";
    internal const string LandmarkPrefix = "__unclaimable_internal_landmark_rule__:";
    internal const string EventPrefix = "__unclaimable_internal_event_rule__:";
    internal const string AwardPrefix = "__unclaimable_internal_award_rule__:";
    internal const string FictionalCharacterPrefix = "__unclaimable_internal_fictional_character_rule__:";
    internal const string FranchisePrefix = "__unclaimable_internal_franchise_rule__:";
    internal const string ProfessionPrefix = "__unclaimable_internal_profession_rule__:";
    internal const string MilitaryPrefix = "__unclaimable_internal_military_rule__:";

    internal static readonly string[] Nationalities =
    {
        "american", "argentine", "argentinian", "australian", "austrian", "belgian", "brazilian", "british",
        "canadian", "chilean", "chinese", "colombian", "croatian", "cuban", "czech", "danish", "dutch",
        "egyptian", "english", "estonian", "ethiopian", "finnish", "french", "german", "ghanaian", "greek",
        "hungarian", "icelandic", "indian", "indonesian", "iranian", "iraqi", "irish", "israeli", "italian",
        "jamaican", "japanese", "jordanian", "kenyan", "korean", "lebanese", "malaysian", "mexican",
        "moroccan", "nepalese", "newzealander", "nigerian", "norwegian", "pakistani", "palestinian", "peruvian",
        "filipino", "filipina", "polish", "portuguese", "romanian", "russian", "saudi", "scottish", "serbian",
        "singaporean", "slovak", "slovenian", "southafrican", "spanish", "swedish", "swiss", "syrian",
        "taiwanese", "thai", "turkish", "ukrainian", "venezuelan", "vietnamese", "welsh"
    };

    internal static readonly string[] Currencies =
    {
        "aed", "afghani", "ars", "aud", "australiandollar", "baht", "bdt", "bitcoin", "btc", "cad",
        "canadiandollar", "chf", "chineseyuan", "cny", "cryptocurrency", "dirham", "dollar", "dong", "egp",
        "ethereum", "eth", "eur", "euro", "gbp", "hkd", "hongkongdollar", "idr", "ils", "indianrupee",
        "inr", "japaneseyen", "jpy", "krone", "krona", "kwd", "lira", "mexicanpeso", "mxn", "naira",
        "ngn", "nok", "nzd", "newzealanddollar", "peso", "php", "pkr", "pln", "pound", "poundsterling",
        "qar", "rand", "riyal", "rub", "rupee", "sar", "sek", "sgd", "singaporedollar", "solana", "sterling",
        "swissfranc", "tether", "thb", "try", "turkishlira", "uah", "usd", "usdc", "usdt", "won", "yen",
        "yuan", "zar", "zloty"
    };

    internal static readonly string[] Religions =
    {
        "anglican", "anglicanism", "bahai", "baptist", "buddhism", "buddhist", "catholic", "catholicism",
        "christian", "christianity", "confucianism", "easternorthodox", "evangelical", "hindu", "hinduism",
        "islam", "islamic", "jain", "jainism", "jehovahswitness", "judaism", "jewish", "latterdaysaints",
        "lutheran", "lutheranism", "methodism", "methodist", "mormon", "orthodoxchristianity", "pentecostal",
        "protestant", "protestantism", "rastafari", "scientology", "shia", "shiism", "shinto", "sikh",
        "sikhism", "sunni", "sunnism", "taoism", "unitarian", "zoroastrian", "zoroastrianism"
    };

    internal static readonly string[] Landmarks =
    {
        "acropolis", "alhambra", "angkorwat", "bigben", "blue-mosque", "bluemosque", "brandenburg-gate",
        "brandenburggate", "burjkhalifa", "chichenitza", "christtheredeemer", "colosseum", "eiffeltower",
        "empirestatebuilding", "forbiddencity", "goldengatebridge", "grandcanyon", "greatpyramid", "greatpyramidofgiza",
        "greatsphinx", "greatwall", "greatwallofchina", "hagia-sophia", "hagiasophia", "hollywoodsign", "louvre",
        "machupicchu", "mountfuji", "mount-rushmore", "mountrushmore", "notredame", "petra", "pyramids",
        "pyramidsofgiza", "sagradafamilia", "space-needle", "spaceneedle", "statueofliberty", "stonehenge",
        "sydneyharbourbridge", "sydneyoperahouse", "tajmahal", "towerbridge", "trevifountain", "uluru",
        "vatican", "victoriafalls", "whitehouse"
    };

    internal static readonly string[] Events =
    {
        "asian-games", "asiangames", "burningman", "cannesfilmfestival", "ces", "coachella", "comiccon",
        "commonwealthgames", "cricketworldcup", "daytona500", "eurovision", "eurovisionsongcontest", "fifaworldcup",
        "gamescom", "indy500", "kentuckyderby", "lemans", "monacograndprix", "olympics", "oktoberfest",
        "panamericangames", "paralympics", "rydercup", "rugbyworldcup", "sundance", "sundancefilmfestival",
        "superbowl", "sxsw", "themasters", "tourdefrance", "venicefilmfestival", "websummit", "wimbledon",
        "worldcup", "worldseries", "wrestlemania"
    };

    internal static readonly string[] Awards =
    {
        "academyaward", "academyawards", "bafta", "baftaawards", "ballondor", "bookerprize", "britawards",
        "emmy", "emmys", "emmyawards", "fieldsmedal", "goldenglobe", "goldenglobes", "grammy", "grammys",
        "grammyawards", "hugoaward", "hugoawards", "laureus", "mtvvideomusicawards", "nobel", "nobelpeaceprize",
        "nobelprize", "nobelprizes", "oscar", "oscars", "palmedor", "peabody", "peabodyawards", "pulitzer",
        "pulitzerprize", "screenactorsguildawards", "tony", "tonys", "tonyawards", "turingaward"
    };

    internal static readonly string[] FictionalCharacters =
    {
        "ariel", "batman", "blackpanther", "captainamerica", "catwoman", "cinderella", "darthvader", "deadpool",
        "donaldduck", "elsa", "frodo", "gandalf", "goku", "hanniballecter", "harleyquinn", "hermionegranger",
        "homer-simpson", "homersimpson", "indianajones", "ironman", "jamesbond", "joker", "katnisseverdeen",
        "link", "lukeskywalker", "mariobros", "mickeymouse", "naruto", "peterpan", "pikachu", "popeye",
        "princessleia", "rockybalboa", "sailormoon", "scoobydoo", "sherlockholmes", "shrek", "sonic",
        "spiderman", "spongebob", "superman", "terminator", "thanos", "thor", "tinkerbell", "wonderwoman",
        "wolverine", "yoda", "zelda"
    };

    internal static readonly string[] Franchises =
    {
        "assassinscreed", "avatar", "barbie", "batmanfranchise", "callofduty", "dccomics", "disney", "dragonball",
        "dungeonsanddragons", "fallout", "fastandfurious", "finalfantasy", "fortnite", "frozen", "gameofthrones",
        "grandtheftauto", "halo", "harrypotter", "hellokitty", "jurassicpark", "lego", "lordoftherings",
        "marvel", "marvelcinematicuniverse", "minecraft", "mortal-kombat", "mortalkombat", "naruto-franchise",
        "nintendo", "onepiece", "pokemon", "resident-evil", "residentevil", "sonicthehedgehog", "star-trek",
        "starwars", "startrek", "streetfighter", "supermario", "thelegendofzelda", "transformers", "warcraft",
        "warhammer", "witcher", "xmen", "zelda-franchise"
    };

    internal static readonly string[] Professions =
    {
        "accountant", "architect", "attorney", "auditor", "barrister", "counselor", "dentist", "doctor",
        "engineer", "financialadvisor", "firefighter", "journalist", "judge", "lawyer", "medic", "nurse",
        "optometrist", "paramedic", "pharmacist", "physician", "pilot", "police", "policeofficer", "professor",
        "psychiatrist", "psychologist", "reporter", "researcher", "scientist", "socialworker", "solicitor", "surgeon",
        "teacher", "therapist", "veterinarian"
    };

    internal static readonly string[] Military =
    {
        "admiral", "airforce", "airman", "armedforces", "army", "brigadier", "captain", "chiefpettyofficer",
        "coastguard", "colonel", "commander", "corporal", "defenceforce", "defenseforce", "fieldmarshal", "general",
        "lieutenant", "major", "marine", "marines", "military", "navy", "officer", "pettyofficer", "private",
        "rearadmiral", "sailor", "sergeant", "soldier", "spaceforce", "squadronleader", "viceadmiral",
        "warrantofficer"
    };
}
