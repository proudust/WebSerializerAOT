# WebSerializer.AOT

NativeAOT support for [Cysharp/WebSerializer](https://github.com/Cysharp/WebSerializer) via source generation.

## Installation

Supporting platform is .NET 8+.

> PM> Install-Package [Proudust.WebSerializer.AOT](https://www.nuget.org/packages/Proudust.WebSerializer.AOT)

## Quick Start

Serializers are generated at compile time, similar to [System.Text.Json source generation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation).

1. Generate `WebSerializerProvider`.

```cs
using Proudust.Web;

// Serialization target
public record Request(string? sortBy, SortDirection direction, int currentPage);

public enum SortDirection
{
    Default,
    Asc,
    Desc
}

// Mark the formatter and provider to be generated for serialization.
[WebSerializable<Request>]
sealed partial class GenerateWebSerializerProvider;
```

2. Use `WebSerializer` with the generated provider.

```cs
using Cysharp.Web;
using Cysharp.Web.Providers;

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

var req = new Request(sortBy: "id", direction: SortDirection.Desc, currentPage: 3);

// sortBy=id&direction=Desc&currentPage=3
var q = WebSerializer.ToQueryString(req, options);
```

## License

This library is licensed under the MIT License.
