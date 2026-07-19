using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;

namespace Proudust.Web;

public sealed class ProviderType
{
    public string? Namespace { get; }

    public (string TypeKeyword, string Name)[] Parents { get; }

    public string Name { get; }

    public string FullyQualifiedName { get; }

    public TargetType[] TargetTypes { get; }

    private readonly KnownTypeSymbols _knownSymbols;

    public ProviderType(INamedTypeSymbol symbol, KnownTypeSymbols knownSymbols)
    {
        _knownSymbols = knownSymbols;

        Namespace = symbol.ContainingNamespace switch
        {
            { IsGlobalNamespace: false } ns => $"{ns}",
            _ => null,
        };
        Parents = symbol.EnumerateContainingTypes()
            .Select(static symbol =>
            {
                string typeKeyword = symbol.GetTypeKeyword();
                string name = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                return (typeKeyword, name);
            })
            .Reverse()
            .ToArray();
        Name = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        FullyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        TargetTypes = symbol.GetAttributes()
            .Where(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass?.ConstructedFrom, _knownSymbols.WebSerializableGenericAttributeType)
                || SymbolEqualityComparer.Default.Equals(x.AttributeClass, _knownSymbols.WebSerializableNonGenericAttributeType))
            .Select(x => new TargetType(x switch
            {
                { AttributeClass.TypeArguments: [INamedTypeSymbol typeSymbol, ..] } => typeSymbol,
                { ConstructorArguments: [{ Value: INamedTypeSymbol typeSymbol }, ..] } => typeSymbol,
                _ => Throw(),
            }, _knownSymbols))
            .ToArray();

        [DoesNotReturn]
        static INamedTypeSymbol Throw()
        {
            throw new InvalidOperationException("Missing type argument for [WebSerializable].");
        }
    }
}

public sealed class TargetType
{
    public string? Prefix { get; }

    public string Name { get; }

    public TargetTypeMember[] Members { get; }

    private readonly KnownTypeSymbols _knownSymbols;

    public TargetType(INamedTypeSymbol symbol, KnownTypeSymbols knownSymbols)
    {
        _knownSymbols = knownSymbols;

        Prefix = symbol.GetAttributes()
            .FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, _knownSymbols.DataContractAttributeType))
            ?.GetNamedArgument<string>(nameof(DataContractAttribute.Namespace));
        Name = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        Members = symbol
            .GetAllMembers()
            .Where(static x => x is
            {
                IsStatic: false,
                IsImplicitlyDeclared: false,
                CanBeReferencedByName: true,
            } and (IFieldSymbol
            {
                IsConst: false,
                DeclaredAccessibility: Accessibility.Public,
            } or IPropertySymbol
            {
                IsIndexer: false,
                GetMethod: not null,
            }))
            // Mimic reflection
            .Select(static (member, index) => (member, index))
            .GroupBy(static x => x.member switch
            {
                IPropertySymbol p => $"P:{p.Name}:{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}",
                _ => $"F:{x.index}",
            })
            .Select(static group => group.Last().member)
            .Where(x => x.DeclaredAccessibility is Accessibility.Public
                && x.GetAttribute(_knownSymbols.IgnoreWebSerializeAttributeType, walkOverrides: true) is null)
            .Select(member => new TargetTypeMember(member, symbol, _knownSymbols))
            .OrderBy(static member => member.Order)
            .ToArray();
    }
}

public sealed class TargetTypeMember
{
    public int Order { get; }

    public string Type { get; }

    public bool IsNullable { get; }

    public string ValueAccess { get; }

    public UnsafeGetterAccessor? GetterAccessor { get; }

    public string SerializedName { get; }

    public string? WebSerializer { get; }

    private readonly KnownTypeSymbols _knownSymbols;

    public TargetTypeMember(ISymbol symbol, INamedTypeSymbol targetType, KnownTypeSymbols knownSymbols)
    {
        _knownSymbols = knownSymbols;

        var dataMemberAttr = symbol.GetAttribute(knownSymbols.DataMemberAttributeType, walkOverrides: false);
        var webSerializerAttr = symbol.GetAttribute(knownSymbols.WebSerializerAttributeType, walkOverrides: true);

        var memberType = symbol switch
        {
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => throw new InvalidOperationException($"Unsupported member kind: {symbol.Kind}"),
        };

        Order = dataMemberAttr?.GetNamedArgument(nameof(DataMemberAttribute.Order), defaultValue: -1) ?? int.MaxValue;
        Type = memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        IsNullable = memberType is { IsValueType: false } or { Name: "Nullable" };
        SerializedName = dataMemberAttr?.GetNamedArgument<string>(nameof(DataMemberAttribute.Name)) ?? symbol.Name;
        WebSerializer = webSerializerAttr?.ConstructorArguments[0] switch
        {
            { Value: INamedTypeSymbol webSerializerType } => webSerializerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            _ => null,
        };

        var declaringType = symbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (symbol is IPropertySymbol
            {
                GetMethod:
                {
                    DeclaredAccessibility: not Accessibility.Public
                } getter
            })
        {
            var returnType = memberType.IsValueType ? Type : Type + "?";
            var accessorName = $"Getter_{symbol.ContainingType.Name}_{symbol.Name}";
            var refModifier = targetType.IsValueType ? "ref " : "";
            GetterAccessor = new UnsafeGetterAccessor(getter.Name, returnType, accessorName, refModifier, declaringType);
            ValueAccess = $"{accessorName}({refModifier}value)";
        }
        else if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingType, targetType))
        {
            ValueAccess = $"(({declaringType})value).{symbol.Name}";
        }
        else
        {
            ValueAccess = $"value.{symbol.Name}";
        }
    }
}

public sealed record UnsafeGetterAccessor(
    string MetadataName,
    string ReturnType,
    string Name,
    string RefModifier,
    string ParameterType
);
