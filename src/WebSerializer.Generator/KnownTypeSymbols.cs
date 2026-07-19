using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;

namespace Proudust.Web;

public sealed class KnownTypeSymbols(Compilation compilation)
{
    public Compilation Compilation { get; } = compilation;

    public INamedTypeSymbol DataContractAttributeType { get; } = ResolveType(compilation, typeof(DataContractAttribute));

    public INamedTypeSymbol DataMemberAttributeType { get; } = ResolveType(compilation, typeof(DataMemberAttribute));

    public INamedTypeSymbol WebSerializerAttributeType { get; } = ResolveType(compilation, "Cysharp.Web.WebSerializerAttribute");

    public INamedTypeSymbol IgnoreWebSerializeAttributeType { get; } = ResolveType(compilation, "Cysharp.Web.IgnoreWebSerializeAttribute");

    public INamedTypeSymbol WebSerializableGenericAttributeType { get; } = ResolveType(compilation, "Proudust.Web.WebSerializableAttribute`1");

    public INamedTypeSymbol WebSerializableNonGenericAttributeType { get; } = ResolveType(compilation, "Proudust.Web.WebSerializableAttribute");

    private static INamedTypeSymbol ResolveType(Compilation compilation, Type type)
    {
        return ResolveType(compilation, type.FullName);
    }

    private static INamedTypeSymbol ResolveType(Compilation compilation, string fullyQualifiedName)
    {
        var type = compilation.GetTypeByMetadataName(fullyQualifiedName);
        if (type is not null)
        {
            return type;
        }

        Throw(fullyQualifiedName);
        return null;

        [DoesNotReturn]
        static void Throw(string fullyQualifiedName)
        {
            throw new InvalidOperationException($"Type {fullyQualifiedName} is not found in compilation.");
        }
    }
}
