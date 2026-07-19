using Microsoft.CodeAnalysis;

namespace Proudust.Web;

public static class RoslynExtensions
{
    public static AttributeData? GetAttribute(this ISymbol symbol, INamedTypeSymbol attributeType, bool walkOverrides)
    {
        for (ISymbol? current = symbol; current is not null; current = walkOverrides ? (current as IPropertySymbol)?.OverriddenProperty : null)
        {
            var attribute = current.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, attributeType));
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    public static IEnumerable<INamedTypeSymbol> EnumerateContainingTypes(this ISymbol symbol)
    {
        INamedTypeSymbol? containingType = symbol.ContainingType;
        while (containingType is not null)
        {
            yield return containingType;
            containingType = containingType.ContainingType;
        }
    }

    public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol symbol)
    {
        if (symbol.BaseType != null)
        {
            foreach (var member in GetAllMembers(symbol.BaseType))
            {
                yield return member;
            }
        }

        foreach (var member in symbol.GetMembers())
        {
            yield return member;
        }
    }

    public static T? GetNamedArgument<T>(this AttributeData attribute, string key, T? defaultValue = default)
    {
        return attribute.NamedArguments.FirstOrDefault(x => x.Key == key).Value switch
        {
            { Value: T value } => value,
            _ => defaultValue,
        };
    }

    public static string GetTypeKeyword(this INamedTypeSymbol symbol)
    {
        return symbol switch
        {
            { IsRecord: true, IsValueType: true } => "record struct",
            { IsRecord: true, IsValueType: false } => "record",
            { IsRecord: false, IsValueType: true } => "struct",
            { IsRecord: false, IsValueType: false } => "class",
        };
    }
}
