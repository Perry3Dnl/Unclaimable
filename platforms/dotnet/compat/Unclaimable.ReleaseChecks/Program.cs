using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: Unclaimable.ReleaseChecks <v0.3.0-output-directory> <v0.4.0-output-directory> <current-output-directory>");
    return 2;
}

var legacyDirectory = Path.GetFullPath(args[0]);
var baseline040Directory = Path.GetFullPath(args[1]);
var currentDirectory = Path.GetFullPath(args[2]);
var failures = new List<string>();

CompareAssemblyApi("Unclaimable.dll", baseline040Directory, currentDirectory, failures);
CompareAssemblyApi("Unclaimable.AspNetCore.dll", baseline040Directory, currentDirectory, failures);
CompareDefault040Behavior(baseline040Directory, currentDirectory, failures);
CompareLegacyCompatibleBehavior(legacyDirectory, currentDirectory, failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine($"0.5.0 release compatibility checks failed with {failures.Count} difference(s):");
    foreach (var failure in failures.Take(100))
    {
        Console.Error.WriteLine($"- {failure}");
    }

    if (failures.Count > 100)
    {
        Console.Error.WriteLine($"- ... and {failures.Count - 100} more difference(s).");
    }

    return 1;
}

Console.WriteLine("Public API compatibility against v0.4.0 passed.");
Console.WriteLine("Default behavior matched v0.4.0 across the compatibility corpus.");
Console.WriteLine("Legacy-compatible valid-Unicode behavior still matched v0.3.0 with the 0.4.0 strict additions explicitly disabled.");
return 0;

static void CompareAssemblyApi(
    string assemblyFile,
    string baselineDirectory,
    string currentDirectory,
    List<string> failures)
{
    using var baseline = new LoadedAssembly(baselineDirectory, assemblyFile);
    using var current = new LoadedAssembly(currentDirectory, assemblyFile);

    var baselineSurface = GetPublicSurface(baseline.Assembly);
    var currentSurface = GetPublicSurface(current.Assembly);

    foreach (var signature in baselineSurface.Except(currentSurface, StringComparer.Ordinal))
    {
        failures.Add($"{assemblyFile}: removed or changed v0.4.0 public API: {signature}");
    }
}

static SortedSet<string> GetPublicSurface(Assembly assembly)
{
    var surface = new SortedSet<string>(StringComparer.Ordinal);

    foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
    {
        surface.Add(TypeSignature(type));

        if (type.IsEnum)
        {
            foreach (var name in Enum.GetNames(type))
            {
                var value = Enum.Parse(type, name);
                surface.Add($"ENUM|{TypeName(type)}|{name}|{Convert.ToUInt64(value, CultureInfo.InvariantCulture)}");
            }
        }

        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            surface.Add($"CTOR|{TypeName(type)}|{Parameters(constructor.GetParameters())}");
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                     .Where(method => !method.IsSpecialName))
        {
            var extension = method.IsDefined(typeof(ExtensionAttribute), inherit: false) ? "extension" : "normal";
            surface.Add(
                $"METHOD|{TypeName(type)}|{extension}|{(method.IsStatic ? "static" : "instance")}|{method.Name}`{method.GetGenericArguments().Length}|{TypeName(method.ReturnType)}|{Parameters(method.GetParameters())}");
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            var getter = property.GetMethod?.IsPublic == true ? "get" : "-";
            var setter = property.SetMethod?.IsPublic == true ? "set" : "-";
            surface.Add(
                $"PROPERTY|{TypeName(type)}|{property.Name}|{TypeName(property.PropertyType)}|{getter}|{setter}|{Parameters(property.GetIndexParameters())}");
        }

        foreach (var eventInfo in type.GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            surface.Add($"EVENT|{TypeName(type)}|{eventInfo.Name}|{TypeName(eventInfo.EventHandlerType!)}");
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                     .Where(field => !type.IsEnum || !field.IsLiteral))
        {
            var constant = field.IsLiteral ? $"const={FormatDefault(field.GetRawConstantValue())}" : "field";
            surface.Add($"FIELD|{TypeName(type)}|{field.Name}|{TypeName(field.FieldType)}|{(field.IsStatic ? "static" : "instance")}|{constant}");
        }
    }

    return surface;
}

static string TypeSignature(Type type)
{
    var kind = type.IsEnum ? "enum"
        : type.IsInterface ? "interface"
        : type.IsValueType ? "struct"
        : "class";
    var modifiers = $"abstract={type.IsAbstract};sealed={type.IsSealed}";
    var baseType = type.BaseType is null ? "-" : TypeName(type.BaseType);
    var interfaces = string.Join(",", type.GetInterfaces().Select(TypeName).OrderBy(value => value, StringComparer.Ordinal));
    return $"TYPE|{TypeName(type)}|{kind}|{modifiers}|base={baseType}|interfaces={interfaces}";
}

static string Parameters(ParameterInfo[] parameters) =>
    string.Join(",", parameters.Select(parameter =>
    {
        var direction = parameter.IsOut ? "out"
            : parameter.ParameterType.IsByRef && parameter.IsIn ? "in"
            : parameter.ParameterType.IsByRef ? "ref"
            : "value";
        var paramsArray = parameter.IsDefined(typeof(ParamArrayAttribute), inherit: false) ? "params" : "normal";
        var optional = parameter.IsOptional
            ? $"optional={FormatDefault(parameter.DefaultValue)}"
            : "required";
        return $"{parameter.Name}:{TypeName(parameter.ParameterType)}:{direction}:{paramsArray}:{optional}";
    }));

static string TypeName(Type type)
{
    if (type.IsByRef)
    {
        return TypeName(type.GetElementType()!) + "&";
    }

    if (type.IsArray)
    {
        return TypeName(type.GetElementType()!) + "[]";
    }

    if (type.IsGenericParameter)
    {
        return "!" + type.Name;
    }

    if (type.IsGenericType)
    {
        var definitionName = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var tick = definitionName.IndexOf('`');
        if (tick >= 0)
        {
            definitionName = definitionName[..tick];
        }

        return $"{definitionName}<{string.Join(",", type.GetGenericArguments().Select(TypeName))}>";
    }

    return type.FullName ?? type.Name;
}

static string FormatDefault(object? value)
{
    if (value is null)
    {
        return "null";
    }

    if (value == DBNull.Value || value == Missing.Value)
    {
        return value.GetType().Name;
    }

    return value switch
    {
        string text => JsonSerializer.Serialize(text),
        char character => ((int)character).ToString(CultureInfo.InvariantCulture),
        bool boolean => boolean ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };
}

static void CompareDefault040Behavior(string baselineDirectory, string currentDirectory, List<string> failures)
{
    using var baseline = new CheckerRuntime(baselineDirectory);
    using var current = new CheckerRuntime(currentDirectory);

    foreach (var value in Build040Corpus())
    {
        var baselineSnapshot = baseline.Snapshot(value, include040Properties: true);
        var currentSnapshot = current.Snapshot(value, include040Properties: true);

        if (!string.Equals(baselineSnapshot, currentSnapshot, StringComparison.Ordinal))
        {
            failures.Add(
                $"default behavior changed for {JsonSerializer.Serialize(value)}: v0.4.0={baselineSnapshot}; current={currentSnapshot}");
        }
    }
}

static void CompareLegacyCompatibleBehavior(string legacyDirectory, string currentDirectory, List<string> failures)
{
    using var baseline = new CheckerRuntime(legacyDirectory);
    using var current = new CheckerRuntime(currentDirectory, useLegacy040Settings: true);

    foreach (var value in BuildValidUnicodeCorpus())
    {
        var baselineSnapshot = baseline.Snapshot(value, include040Properties: false);
        var currentSnapshot = current.Snapshot(value, include040Properties: false);

        if (!string.Equals(baselineSnapshot, currentSnapshot, StringComparison.Ordinal))
        {
            failures.Add(
                $"legacy-compatible behavior changed for {JsonSerializer.Serialize(value)}: v0.3.0={baselineSnapshot}; current={currentSnapshot}");
        }
    }
}

static IReadOnlyList<string?> Build040Corpus()
{
    var values = BuildValidUnicodeCorpus().ToList();
    values.Add(new string(new[] { '\uD800' }));
    values.Add(new string(new[] { '\uDC00' }));
    values.Add("ab" + new string(new[] { '\uD800' }) + "cd");
    return values;
}

static IReadOnlyList<string?> BuildValidUnicodeCorpus()
{
    var values = new List<string?>
    {
        null,
        string.Empty,
        "a",
        "ab",
        "abc",
        "ordinaryuser",
        "OrdinaryUser",
        "admin",
        "ADMIN",
        "supportive",
        "customer-service",
        "john_doe",
        ".john",
        "john.",
        "ordinary2",
        "user\u0661",
        new string('a', 33),
        "café",
        "cafe\u0301",
        "ａｄｍｉｎ",
        "ab😀cd",
        "δοκιμή",
        "пользователь",
        "مستخدم",
        "用户名称",
        "ab\u200Dcd",
        "ab\u0001cd",
        "\u0301\u0301\u0301",
        "\u200D\u200C\u2060",
        " admin ",
        "N1ke",
        "G00gle",
        "r00t",
        "root-user",
        "moderator.test"
    };

    var random = new Random(40300);
    var atoms = new[]
    {
        "a", "b", "c", "x", "y", "z", "A", "M", "Q",
        "é", "ø", "α", "β", "а", "я", "中", "名", "😀",
        "\u0301", "\u200D", "-", "_", ".", " ", "1", "@", "$", "!"
    };

    for (var item = 0; item < 1200; item++)
    {
        var atomCount = random.Next(1, 13);
        var builder = new StringBuilder();
        for (var index = 0; index < atomCount; index++)
        {
            builder.Append(atoms[random.Next(atoms.Length)]);
        }

        values.Add(builder.ToString());
    }

    for (var item = 0; item < 600; item++)
    {
        var length = random.Next(1, 25);
        var chars = new char[length];
        for (var index = 0; index < chars.Length; index++)
        {
            chars[index] = (char)random.Next('a', 'z' + 1);
        }

        values.Add(new string(chars));
    }

    return values;
}

sealed class CheckerRuntime : IDisposable
{
    private readonly IsolatedLoadContext _context;
    private readonly object _checker;
    private readonly MethodInfo _check;
    private readonly MethodInfo _checkDetailed;

    public CheckerRuntime(string directory, bool useLegacy040Settings = false)
    {
        _context = new IsolatedLoadContext(directory);
        var assembly = _context.LoadFromAssemblyPath(Path.Combine(directory, "Unclaimable.dll"));
        var checkerType = assembly.GetType("Unclaimable.Checker", throwOnError: true)!;

        if (useLegacy040Settings)
        {
            var optionsType = assembly.GetType("Unclaimable.Options", throwOnError: true)!;
            var options = Activator.CreateInstance(optionsType)!;
            optionsType.GetProperty("RejectInvisibleOnlyIdentifiers")!.SetValue(options, false);
            optionsType.GetProperty("RejectControlCharacters")!.SetValue(options, false);
            optionsType.GetProperty("RejectFormatCharacters")!.SetValue(options, false);
            optionsType.GetProperty("ConsistentCompactMatching")!.SetValue(options, false);
            _checker = checkerType.GetConstructor(new[] { optionsType })!.Invoke(new[] { options });
        }
        else
        {
            _checker = Activator.CreateInstance(checkerType)!;
        }

        _check = checkerType.GetMethod("Check", new[] { typeof(string) })!;
        _checkDetailed = checkerType.GetMethod("CheckDetailed", new[] { typeof(string), typeof(bool) })!;
    }

    public string Snapshot(string? value, bool include040Properties)
    {
        try
        {
            var result = _check.Invoke(_checker, new object?[] { value })!;
            var detailed = _checkDetailed.Invoke(_checker, new object?[] { value, false })!;
            return SnapshotResult(result, include040Properties) + "|D=" + SnapshotDetailed(detailed, include040Properties);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            return "EX:" + exception.InnerException.GetType().FullName;
        }
    }

    public void Dispose() => _context.Unload();

    private static string SnapshotResult(object result, bool include040Properties)
    {
        var type = result.GetType();
        var parts = new List<string>
        {
            "R=" + Get(type, result, "IsReserved"),
            "C=" + Get(type, result, "IsClaimable"),
            "I=" + Encode((string?)type.GetProperty("Input")!.GetValue(result)),
            "IL=" + Get(type, result, "InputLength"),
            "MV=" + Encode((string?)type.GetProperty("MatchedValue")!.GetValue(result)),
            "CAT=" + Encode((string?)type.GetProperty("Category")!.GetValue(result)),
            "K=" + Convert.ToInt32(type.GetProperty("MatchKind")!.GetValue(result), CultureInfo.InvariantCulture),
            "OI=" + NullableValue(type, result, "OffendingCharacterIndex"),
            "OC=" + Encode((string?)type.GetProperty("OffendingCharacter")!.GetValue(result)),
            "MS=" + NullableValue(type, result, "MatchStartIndex"),
            "ML=" + NullableValue(type, result, "MatchLength")
        };

        if (include040Properties)
        {
            parts.Add("OMS=" + NullableValue(type, result, "OriginalMatchStartIndex"));
            parts.Add("OML=" + NullableValue(type, result, "OriginalMatchLength"));
            parts.Add("LL=" + NullableValue(type, result, "LengthLimit"));
        }

        return string.Join(";", parts);
    }

    private static string SnapshotDetailed(object detailed, bool include040Properties)
    {
        var type = detailed.GetType();
        var diagnostics = (System.Collections.IEnumerable)type.GetProperty("Diagnostics")!.GetValue(detailed)!;
        var parts = new List<string>
        {
            "R=" + Get(type, detailed, "IsReserved"),
            "C=" + Get(type, detailed, "IsClaimable"),
            "I=" + Encode((string?)type.GetProperty("Input")!.GetValue(detailed)),
            "IL=" + Get(type, detailed, "InputLength")
        };

        foreach (var diagnostic in diagnostics)
        {
            var diagnosticType = diagnostic!.GetType();
            var diagnosticParts = new List<string>
            {
                "K=" + Convert.ToInt32(diagnosticType.GetProperty("Kind")!.GetValue(diagnostic), CultureInfo.InvariantCulture),
                "MV=" + Encode((string?)diagnosticType.GetProperty("MatchedValue")!.GetValue(diagnostic)),
                "CAT=" + Encode((string?)diagnosticType.GetProperty("Category")!.GetValue(diagnostic)),
                "OI=" + NullableValue(diagnosticType, diagnostic, "OffendingCharacterIndex"),
                "OC=" + Encode((string?)diagnosticType.GetProperty("OffendingCharacter")!.GetValue(diagnostic)),
                "MS=" + NullableValue(diagnosticType, diagnostic, "MatchStartIndex"),
                "ML=" + NullableValue(diagnosticType, diagnostic, "MatchLength")
            };

            if (include040Properties)
            {
                diagnosticParts.Add("OMS=" + NullableValue(diagnosticType, diagnostic, "OriginalMatchStartIndex"));
                diagnosticParts.Add("OML=" + NullableValue(diagnosticType, diagnostic, "OriginalMatchLength"));
            }

            diagnosticParts.Add("MSG=" + Encode((string?)diagnosticType.GetProperty("Message")!.GetValue(diagnostic)));
            parts.Add(string.Join(",", diagnosticParts));
        }

        return string.Join("/", parts);
    }

    private static string Get(Type type, object instance, string property) =>
        Convert.ToString(type.GetProperty(property)!.GetValue(instance), CultureInfo.InvariantCulture) ?? string.Empty;

    private static string NullableValue(Type type, object instance, string property)
    {
        var value = type.GetProperty(property)!.GetValue(instance);
        return value is null ? "null" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string Encode(string? value) =>
        value is null ? "null" : Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
}

sealed class LoadedAssembly : IDisposable
{
    private readonly IsolatedLoadContext _context;

    public LoadedAssembly(string directory, string assemblyFile)
    {
        _context = new IsolatedLoadContext(directory);
        Assembly = _context.LoadFromAssemblyPath(Path.Combine(directory, assemblyFile));
    }

    public Assembly Assembly { get; }

    public void Dispose() => _context.Unload();
}

sealed class IsolatedLoadContext : AssemblyLoadContext
{
    private readonly string _directory;

    public IsolatedLoadContext(string directory)
        : base(isCollectible: true)
    {
        _directory = directory;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var localPath = Path.Combine(_directory, assemblyName.Name + ".dll");
        return File.Exists(localPath) ? LoadFromAssemblyPath(localPath) : null;
    }
}
