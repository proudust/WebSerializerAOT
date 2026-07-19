using Microsoft.CodeAnalysis;

namespace Proudust.Web;

internal static class DiagnosticDescriptors
{
    const string Category = "WebSerializerAOT";

    public static readonly DiagnosticDescriptor TargetFrameworkTooLow = new(
        id: "WSAOT001",
        title: "WebSerializer.AOT requires .NET 8+",
        messageFormat: "WebSerializer.AOT requires .NET 8 or later. Code generation was skipped.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
