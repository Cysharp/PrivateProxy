using Microsoft.CodeAnalysis;

namespace PrivateProxy.Generator;

public static class DiagnosticDescriptors
{
    const string Category = "PrivateProxy";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "PP001",
        title: "PrivateProxy type must be partial",
        messageFormat: "The PrivateProxy type '{0}' must be partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotAllowReadOnly = new(
        id: "PP002",
        title: "PrivateProxy does not allow readonly struct",
        messageFormat: "The PrivateProxy struct '{0}' does not allow readonly struct",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ClassNotAllowRefStruct = new(
        id: "PP003",
        title: "PrivateProxy class does not allow ref struct",
        messageFormat: "The PrivateProxy class(reference-type) '{0}' does not allow ref struct",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StructNotAllowClass = new(
        id: "PP004",
        title: "PrivateProxy struct does not allow class",
        messageFormat: "The PrivateProxy struct '{0}' does not allow class, only allows ref struct",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StructNotAllowStruct = new(
        id: "PP005",
        title: "PrivateProxy struct does not allow struct",
        messageFormat: "The PrivateProxy struct '{0}' does not allow struct, only allows ref struct",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RefStructNotSupported = new(
        id: "PP006",
        title: "PrivateProxy does not support ref struct",
        messageFormat: "The PrivateProxy does not support ref struct",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericsNotSupported = new(
        id: "PP007",
        title: "PrivateProxy does not support generics type",
        messageFormat: "The PrivateProxy does not support generics type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

}