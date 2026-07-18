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
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Select(symbol => new TargetTypeMember(symbol, _knownSymbols))
            .OrderBy(static member => member.Order)
            .ToArray();
    }
}

public sealed class TargetTypeMember
{
    public int Order { get; }

    public string Type { get; }

    public bool IsNullable { get; }

    public string MemberName { get; }

    public string SerializedName { get; }

    public string? WebSerializer { get; }

    private readonly KnownTypeSymbols _knownSymbols;

    public TargetTypeMember(IPropertySymbol symbol, KnownTypeSymbols knownSymbols)
    {
        _knownSymbols = knownSymbols;

        var attrs = symbol.GetAttributes();
        var dataMemberAttr = attrs.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, _knownSymbols.DataMemberAttributeType));
        var webSerializerAttr = attrs.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, _knownSymbols.WebSerializerAttributeType));

        Order = dataMemberAttr?.GetNamedArgument(nameof(DataMemberAttribute.Order), int.MaxValue) ?? int.MaxValue;
        Type = symbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        IsNullable = symbol.Type is { IsValueType: false } or { Name: "Nullable" };
        MemberName = symbol.Name;
        SerializedName = dataMemberAttr?.GetNamedArgument<string>(nameof(DataMemberAttribute.Name)) ?? symbol.Name;
        WebSerializer = webSerializerAttr?.ConstructorArguments[0] switch
        {
            { Value: INamedTypeSymbol webSerializerType } => webSerializerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            _ => null,
        };
    }
}
