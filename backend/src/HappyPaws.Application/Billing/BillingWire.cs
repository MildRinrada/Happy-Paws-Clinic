using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Billing;

namespace HappyPaws.Application.Billing;

/// <summary>
/// The wire vocabulary of the billing module, in one place.
/// <para>
/// The contract's state names are not what <c>Enum.ToString()</c> produces —
/// <c>PartiallyPaid</c> has to travel as <c>partially_paid</c> — so the conversion is written once
/// rather than reimplemented per DTO. A second copy that lower-cased the enum would put
/// <c>partiallypaid</c> on the wire and no client would match it.
/// </para>
/// </summary>
public static class BillingWire
{
    private static readonly Dictionary<InvoiceStatus, string> StatusNames = new()
    {
        [InvoiceStatus.Open] = "open",
        [InvoiceStatus.PartiallyPaid] = "partially_paid",
        [InvoiceStatus.Paid] = "paid",
        [InvoiceStatus.Refunded] = "refunded",
        [InvoiceStatus.Void] = "void",
    };

    private static readonly Dictionary<string, PaymentMethod> Methods = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cash"] = PaymentMethod.Cash,
        ["card"] = PaymentMethod.Card,
        ["promptpay"] = PaymentMethod.PromptPay,
    };

    private static readonly Dictionary<string, InvoiceLineKind> LineKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["service"] = InvoiceLineKind.Service,
        ["treatment"] = InvoiceLineKind.Treatment,
        ["medicine"] = InvoiceLineKind.Medicine,
        ["other"] = InvoiceLineKind.Other,
    };

    public static string ToWire(InvoiceStatus status) => StatusNames[status];

    public static string ToWire(PaymentMethod method) =>
        method == PaymentMethod.PromptPay ? "promptpay" : method.ToString().ToLowerInvariant();

    public static string ToWire(InvoiceLineKind kind) => kind.ToString().ToLowerInvariant();

    /// <summary>Parses a <c>?status=</c> filter. Null when absent; a validation failure when unknown.</summary>
    public static InvoiceStatus? ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (var (status, name) in StatusNames)
        {
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase))
            {
                return status;
            }
        }

        throw Invalid("status", "สถานะบิลต้องเป็น open, partially_paid, paid, refunded หรือ void");
    }

    /// <summary>
    /// Parses a tender. There is no default: a payment whose method was not stated cannot be
    /// reconciled against a drawer or an acquirer, and guessing "cash" would silently make the till
    /// wrong.
    /// </summary>
    public static PaymentMethod ParseMethod(string? value) =>
        value is { Length: > 0 } && Methods.TryGetValue(value, out var method)
            ? method
            : throw Invalid("method", "ช่องทางชำระต้องเป็น cash, card หรือ promptpay");

    /// <summary>Parses a line kind, defaulting to <c>other</c> — the bucket that claims nothing.</summary>
    public static InvoiceLineKind ParseLineKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return InvoiceLineKind.Other;
        }

        return LineKinds.TryGetValue(value, out var kind)
            ? kind
            : throw Invalid("lines[].kind", "ประเภทรายการต้องเป็น service, treatment, medicine หรือ other");
    }

    private static ValidationFailedException Invalid(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
