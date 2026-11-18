using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Tools;

/// <summary>
/// Writes a module's CHEATSHEET.md from its built library and documentation file, every public type
/// with its public members a line each, the declaration without its modifiers and the first
/// sentence of its summary.
/// </summary>
/// <remarks>
/// <para>
/// The page's list is the C# block under the line <see cref="Marker"/>, and everything outside it
/// is written by hand and kept. The types stand in the order the README first names each in code,
/// a type it names nowhere after them by name, and each type's members in the order the build
/// declares them, its fields, constructors, properties, methods and events.
/// </para>
/// <para>
/// A line's comment starts at the same column where the declaration is shorter, so a member added
/// or changed moves its own line alone, and runs on to its end on the one line, so a search for a
/// member finds its line whole.
/// </para>
/// </remarks>
public static partial class Cheatsheet
{
    /// <summary>The line the page's written list stands under, followed by its C# block.</summary>
    public const string Marker = "<!-- written by Cheatsheet.Write from the built library -->";

    private const int CommentColumn = 57;

    /// <summary>
    /// The page with its list written again from a built library, the documentation file read from
    /// beside the assembly, or a page of its own where the one given is empty.
    /// </summary>
    /// <param name="library">The module's library, as a test loads it.</param>
    /// <param name="readme">The README's text, which gives the types' order.</param>
    /// <param name="page">The page as it stands, or an empty text for a first one.</param>
    /// <exception cref="InvalidDataException">The page holds no <see cref="Marker"/> line with a C#
    /// block under it.</exception>
    public static string Write(Assembly library, string readme, string page) =>
        Write(library, XDocument.Load(Path.ChangeExtension(library.Location, ".xml")), readme, page);

    /// <summary>The page with its list written again from a built library and its
    /// documentation.</summary> <exception cref="InvalidDataException">The page holds no
    /// <see cref="Marker"/> line with a C# block under it.</exception>
    public static string Write(Assembly library, XDocument documentation, string readme, string page)
    {
        var block = List(library, documentation, readme);
        page = page.ReplaceLineEndings("\n");
        if (page.Trim().Length == 0)
            return $"# {library.GetName().Name} cheatsheet\n\n"
                + "Every public type of the library with its public members, each a line of its declaration and the first sentence\n"
                + "of its summary. The list is written by `Cheatsheet.Write` from the built library, and the text around it by hand.\n\n"
                + $"{Marker}\n```csharp\n{block}```\n";

        var lines = page.Split('\n');
        var at = Array.IndexOf(lines, Marker);
        if (at < 0 || at + 1 >= lines.Length || lines[at + 1] != "```csharp")
            throw new InvalidDataException($"The page holds no line {Marker} with a C# block under it.");
        var end = Array.IndexOf(lines, "```", at + 2);
        if (end < 0)
            throw new InvalidDataException("The page's written C# block is not closed.");
        return string.Join("\n", lines[..(at + 2)]) + "\n" + block + string.Join("\n", lines[end..]);
    }

    /// <summary>The list's lines, a type's after a blank line, each line ending in a line
    /// feed.</summary>
    private static string List(Assembly library, XDocument documentation, string readme)
    {
        var members = documentation.Descendants("member")
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);
        var text = new StringBuilder();
        foreach (var type in Ordered(library, readme))
        {
            if (text.Length > 0) text.Append('\n');
            var described = Summary(members, "T:" + Id(type), null);
            text.Append(Line($"{Kind(type)} {Name(type)}", described));
            // A record's constructor is documented by its type's summary, which its line repeats
            // word for word, so a member saying what its type's line says carries no comment.
            foreach (var member in Members(type))
                text.Append(Line(Declaration(member), Summary(members, Id(member), member) is var summary && summary == described ? "" : summary));
        }

        return text.ToString();
    }

    // The library's public types not nested in another, in the order the README first names each
    // in code, the rest after them by name.
    private static IEnumerable<Type> Ordered(Assembly library, string readme) =>
        library.GetExportedTypes().Where(type => !type.IsNested)
            .Select(type => (Type: type, At: Regex.Match(readme, $@"`{Regex.Escape(Plain(type.Name))}([`.(<\[ ])") is { Success: true } named ? named.Index : int.MaxValue))
            .OrderBy(entry => entry.At).ThenBy(entry => entry.Type.Name, StringComparer.Ordinal)
            .Select(entry => entry.Type);

    private static IEnumerable<MemberInfo> Members(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        bool shown(MemberInfo member) => !member.IsDefined(typeof(CompilerGeneratedAttribute)) && member.Name != "EqualityContract";
        IEnumerable<T> inOrder<T>(IEnumerable<T> members) where T : MemberInfo => members.Where(shown).OrderBy(member => member.MetadataToken);
        return inOrder(type.GetFields(flags).Where(field => !field.IsSpecialName)).Cast<MemberInfo>()
            .Concat(type.IsEnum ? [] : inOrder(type.GetConstructors(flags)))
            .Concat(inOrder(type.GetProperties(flags)))
            .Concat(inOrder(type.GetMethods(flags).Where(method => !method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))))
            .Concat(inOrder(type.GetEvents(flags)));
    }

    private static string Line(string declaration, string summary) =>
        (summary.Length == 0 ? declaration : declaration.PadRight(Math.Max(CommentColumn, declaration.Length + 2)) + "// " + summary) + "\n";

    // -- Declarations

    private static string Kind(Type type) =>
        type.IsEnum ? "enum"
        : type.IsInterface ? "interface"
        : typeof(Delegate).IsAssignableFrom(type.BaseType) ? "delegate"
        : type.IsValueType ? (type.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null ? "record struct" : "struct")
        : type.GetMethod("<Clone>$") is not null ? "record" : "class";

    private static string Declaration(MemberInfo member)
    {
        var nullability = new NullabilityInfoContext();
        switch (member)
        {
            case FieldInfo field when field.DeclaringType!.IsEnum:
                return $"{field.Name} = {Convert.ToInt64(field.GetRawConstantValue())},";
            case FieldInfo field:
                return $"{TypeName(field.FieldType, nullability.Create(field))} {field.Name}" + (field.IsLiteral ? $" = {Value(field.GetRawConstantValue(), field.FieldType)};" : ";");
            case ConstructorInfo constructor:
                return $"{Plain(constructor.DeclaringType!.Name)}({Parameters(constructor, nullability)});";
            case PropertyInfo property:
                var setter = property.SetMethod is { IsPublic: true } set
                    ? set.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit") ? " init;" : " set;"
                    : "";
                var index = property.GetIndexParameters();
                var name = index.Length == 0 ? property.Name : $"this[{string.Join(", ", index.Select(p => Parameter(p, nullability)))}]";
                return $"{TypeName(property.PropertyType, nullability.Create(property))} {name} {{ get;{setter} }}";
            case MethodInfo method:
                var generic = method.IsGenericMethod ? $"<{string.Join(", ", method.GetGenericArguments().Select(a => a.Name))}>" : "";
                var returned = TypeName(method.ReturnType, nullability.Create(method.ReturnParameter));
                var called = method.Name.StartsWith("op_", StringComparison.Ordinal) ? $"operator {method.Name[3..]}" : method.Name;
                var extension = method.IsDefined(typeof(ExtensionAttribute)) ? "this " : "";
                return $"{returned} {called}{generic}({extension}{Parameters(method, nullability)});";
            case EventInfo @event:
                return $"event {TypeName(@event.EventHandlerType!, nullability.Create(@event))} {@event.Name};";
            default:
                return member.Name;
        }
    }

    private static string Parameters(MethodBase method, NullabilityInfoContext nullability) =>
        string.Join(", ", method.GetParameters().Select(parameter => Parameter(parameter, nullability)));

    private static string Parameter(ParameterInfo parameter, NullabilityInfoContext nullability)
    {
        var type = parameter.ParameterType;
        var modifier = parameter.IsDefined(typeof(ParamArrayAttribute)) ? "params "
            : type.IsByRef ? (parameter.IsOut ? "out " : parameter.IsIn ? "in " : "ref ") : "";
        var text = $"{modifier}{TypeName(type.IsByRef ? type.GetElementType()! : type, nullability.Create(parameter))} {parameter.Name}";
        return parameter.HasDefaultValue ? $"{text} = {Value(parameter.RawDefaultValue, type)}" : text;
    }

    private static string Value(object? value, Type type) => value switch
    {
        null => type.IsValueType && Nullable.GetUnderlyingType(type) is null ? "default" : "null",
        string text => $"\"{text}\"",
        bool flag => flag ? "true" : "false",
        char letter => $"'{letter}'",
        // An infinite or undefined default is written as C# names it, where the number's own text,
        // Infinity, reads as no C#.
        float single when float.IsPositiveInfinity(single) => "float.PositiveInfinity",
        float single when float.IsNegativeInfinity(single) => "float.NegativeInfinity",
        float single when float.IsNaN(single) => "float.NaN",
        double number when double.IsPositiveInfinity(number) => "double.PositiveInfinity",
        double number when double.IsNegativeInfinity(number) => "double.NegativeInfinity",
        double number when double.IsNaN(number) => "double.NaN",
        float single => single.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f",
        double number => number.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        _ when (Nullable.GetUnderlyingType(type) ?? type).IsEnum => $"{Plain((Nullable.GetUnderlyingType(type) ?? type).Name)}.{Enum.GetName(Nullable.GetUnderlyingType(type) ?? type, value)}",
        IFormattable number => number.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static readonly Dictionary<Type, string> Aliases = new()
    {
        [typeof(bool)] = "bool", [typeof(byte)] = "byte", [typeof(sbyte)] = "sbyte", [typeof(short)] = "short",
        [typeof(ushort)] = "ushort", [typeof(int)] = "int", [typeof(uint)] = "uint", [typeof(long)] = "long",
        [typeof(ulong)] = "ulong", [typeof(float)] = "float", [typeof(double)] = "double", [typeof(decimal)] = "decimal",
        [typeof(char)] = "char", [typeof(string)] = "string", [typeof(object)] = "object", [typeof(void)] = "void",
    };

    // A type as C# writes it in a declaration, its nullable reference marked where the build says.
    private static string TypeName(Type type, NullabilityInfo? nullability)
    {
        var mark = nullability is { ReadState: NullabilityState.Nullable } && !type.IsValueType ? "?" : "";
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return TypeName(underlying, null) + "?";
        if (type.IsArray)
            return TypeName(type.GetElementType()!, nullability?.ElementType) + "[]" + mark;
        if (Aliases.TryGetValue(type, out var alias))
            return alias + mark;
        if (type.IsGenericParameter)
            return type.Name + mark;
        var arguments = type.IsGenericType ? type.GetGenericArguments() : [];
        var infos = nullability?.GenericTypeArguments ?? [];
        string argument(int i) => TypeName(arguments[i], i < infos.Length ? infos[i] : null);
        if (type.IsGenericType && type.FullName?.StartsWith("System.ValueTuple`", StringComparison.Ordinal) == true)
            return $"({string.Join(", ", arguments.Select((_, i) => argument(i)))})" + mark;
        var name = Name(type);
        return (arguments.Length == 0 ? name : $"{Plain(name)}<{string.Join(", ", arguments.Select((_, i) => argument(i)))}>") + mark;
    }

    private static string Name(Type type)
    {
        var name = type.IsNested ? Name(type.DeclaringType!) + "." + type.Name : type.Name;
        return type.IsGenericTypeDefinition ? $"{Plain(name)}<{string.Join(", ", type.GetGenericArguments().Select(a => a.Name))}>" : name;
    }

    private static string Plain(string name) => name.Split('`')[0];

    // -- Documentation

    // A member's name as the documentation file writes it.
    private static string Id(MemberInfo member)
    {
        var type = Id(member.DeclaringType!);
        return member switch
        {
            FieldInfo field => $"F:{type}.{field.Name}",
            PropertyInfo property => $"P:{type}.{property.Name}{Arguments(property.GetIndexParameters())}",
            EventInfo @event => $"E:{type}.{@event.Name}",
            ConstructorInfo constructor => $"M:{type}.#ctor{Arguments(constructor.GetParameters())}",
            MethodInfo method => $"M:{type}.{method.Name}{(method.IsGenericMethod ? $"``{method.GetGenericArguments().Length}" : "")}{Arguments(method.GetParameters())}",
            _ => "",
        };
    }

    private static string Id(Type type) => (type.IsNested ? Id(type.DeclaringType!) + "." + type.Name : $"{type.Namespace}.{type.Name}").TrimStart('.');

    private static string Arguments(ParameterInfo[] parameters) =>
        parameters.Length == 0 ? "" : $"({string.Join(",", parameters.Select(parameter => ParameterId(parameter.ParameterType)))})";

    private static string ParameterId(Type type)
    {
        if (type.IsByRef) return ParameterId(type.GetElementType()!) + "@";
        if (type.IsArray) return ParameterId(type.GetElementType()!) + "[]";
        if (type.IsGenericParameter) return (type.DeclaringMethod is null ? "`" : "``") + type.GenericParameterPosition;
        if (!type.IsGenericType) return (type.FullName ?? type.Name).Replace('+', '.');
        var definition = type.GetGenericTypeDefinition();
        var name = (definition.FullName ?? definition.Name).Replace('+', '.');
        return $"{name[..name.IndexOf('`')]}{{{string.Join(",", type.GetGenericArguments().Select(ParameterId))}}}";
    }

    // The first sentence of a summary without its full stop, since a line reminds and the README
    // explains, a reference written as the name it refers to. A member whose documentation is
    // inherited takes the summary its inheritdoc's cref names, or else the one of the member it
    // overrides or the interface's it implements, as an editor shows it.
    private static string Summary(Dictionary<string, XElement> members, string id, MemberInfo? member)
    {
        for (var depth = 0; depth < 8 && members.TryGetValue(id, out var documented); depth++)
        {
            if (documented.Element("summary") is { } summary)
            {
                var text = Spaces().Replace(Text(summary), " ").Trim();
                var end = Sentence().Match(text);
                return (end.Success ? text[..end.Index] : text).TrimEnd('.');
            }
            if (documented.Element("inheritdoc") is not { } inherited)
                return "";
            var next = inherited.Attribute("cref")?.Value ?? (member is null ? null : Inherited(member) is { } from ? Id(from) : null);
            if (next is null)
                return "";
            (id, member) = (next, member is null || inherited.Attribute("cref") is not null ? null : Inherited(member));
        }

        return "";
    }

    // The member a member's documentation is inherited from, the one it overrides or else the
    // member of an interface it implements.
    private static MemberInfo? Inherited(MemberInfo member)
    {
        var accessor = member switch
        {
            MethodInfo method => method,
            PropertyInfo property => property.GetMethod ?? property.SetMethod,
            _ => null,
        };
        if (accessor is null || member.DeclaringType is not { } type)
            return null;
        MethodInfo? inherited = accessor.GetBaseDefinition() is var root && root != accessor ? root : null;
        if (inherited is null && !type.IsInterface)
            foreach (var face in type.GetInterfaces())
            {
                var map = type.GetInterfaceMap(face);
                var at = Array.IndexOf(map.TargetMethods, accessor);
                if (at >= 0)
                {
                    inherited = map.InterfaceMethods[at];
                    break;
                }
            }

        if (inherited is null)
            return null;
        if (member is MethodInfo)
            return inherited;
        return inherited.DeclaringType!.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(property => property.GetMethod == inherited || property.SetMethod == inherited);
    }

    private static string Text(XElement element) => string.Concat(element.Nodes().Select(node => node switch
    {
        XText text => text.Value,
        XElement { Name.LocalName: "see" or "seealso" } see when see.Attribute("cref") is { } cref => Referred(cref.Value),
        XElement { Name.LocalName: "see" } see when see.Attribute("langword") is { } word => word.Value,
        XElement { Name.LocalName: "paramref" or "typeparamref" } reference => reference.Attribute("name")?.Value ?? "",
        XElement inner => Text(inner),
        _ => "",
    }));

    private static string Referred(string cref)
    {
        var name = cref.Contains(':') ? cref[(cref.IndexOf(':') + 1)..] : cref;
        name = name.Split('(')[0];
        return Plain(name[(name.LastIndexOf('.') + 1)..]);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    // A sentence ends at a full stop that a blank and a capital or a code's mark follow.
    [GeneratedRegex(@"\.\s+(?=[A-Z`<(])")]
    private static partial Regex Sentence();
}
