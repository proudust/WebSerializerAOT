// See https://aka.ms/new-console-template for more information
using System.Runtime.Serialization;
using Cysharp.Web;
using Cysharp.Web.Providers;
using Proudust.Web;

var options = WebSerializerOptions.Default with
{
    Provider = WebSerializerProvider.Create([
        PrimitiveWebSerializerProvider.Instance,
        BuiltinWebSerializerProvider.Instance,
        AttributeWebSerializerProvider.Instance,
        GenericsWebSerializerProvider.Instance,
        CollectionWebSerializerProvider.Instance,
        ObjectFallbackWebSerializerProvider.Instance,
        // ObjectGraphWebSerializerProvider.Instance, // Does not work in AOT due to reflection.
        GenerateWebSerializerProvider.Instance,
    ]),
};

Console.WriteLine(WebSerializer.ToQueryString("https://www.google.co.jp/search", new UrlParams
{
    Query = "hello",
}, options));

readonly partial struct UrlParams
{
    [DataMember(Name = "q")]
    public string Query { get; init; }
}

[WebSerializable<UrlParams>]
sealed partial class GenerateWebSerializerProvider;
