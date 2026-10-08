using System.Security.Cryptography;

namespace HappyPaws.Domain.Common;

/// <summary>
/// Generates the contract's prefixed identifiers — <c>usr_8fA2</c>, <c>stf_204</c>,
/// <c>apt_24810</c>.
/// <para>
/// The body is time-ordered: the first 48 bits are a millisecond timestamp, the rest is random.
/// That keeps inserts appending to the right of the index instead of scattering across it, and it
/// makes the id itself a usable tiebreaker for cursor pagination.
/// </para>
/// <para>
/// Prefixes are visible in URLs and logs, which is the point — a support ticket quoting
/// <c>pet_...</c> is unambiguous in a way a bare UUID is not. They carry no secret: the random
/// tail is what makes an id unguessable.
/// </para>
/// </summary>
public static class EntityId
{
    /// <summary>Crockford base32 — no I, L, O or U, so ids cannot be misread aloud or in print.</summary>
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public const string User = "usr";
    public const string Staff = "stf";
    public const string Customer = "cus";
    public const string Session = "ses";
    public const string Role = "rol";
    public const string Branch = "brn";
    public const string Pet = "pet";
    public const string Ownership = "own";
    public const string Service = "svc";
    public const string Shift = "shf";
    public const string Appointment = "apt";
    public const string Hold = "hold";
    public const string QueueTicket = "tkt";
    public const string Consultation = "con";
    public const string Vitals = "vit";
    public const string Finding = "fnd";
    public const string Diagnosis = "dx";
    public const string Treatment = "trt";
    public const string Prescription = "rx";
    public const string MedicalRecord = "rec";
    public const string Amendment = "amd";
    public const string Allergy = "alg";
    public const string AuditEntry = "aud";
    public const string Consent = "cns";
    public const string DataRequest = "dsr";
    public const string Invoice = "inv";
    public const string InvoiceLine = "ivl";
    public const string Discount = "dsc";
    public const string Payment = "pay";
    public const string Refund = "rfd";
    public const string Receipt = "rcp";
    /// <summary><c>cin</c>, not <c>inv</c> — that prefix belongs to invoices, and an id a support
    /// ticket quotes has to be unambiguous at a glance.</summary>
    public const string Invitation = "cin";

    /// <summary>A staff invitation — kept apart from <c>cin</c> because accepting one creates a login.</summary>
    public const string StaffInvitation = "sin";
    public const string Holiday = "hol";
    public const string Contact = "cct";
    public const string Article = "art";
    public const string TimeOff = "tof";
    public const string Vaccination = "vac";

    /// <summary>The contract's own example: <c>lab_338</c>.</summary>
    public const string LabRequest = "lab";
    public const string LabResult = "lbr";
    public const string TreatmentTemplate = "tpl";
    public const string WaitingList = "wtl";
    public const string TreatmentPlan = "tpn";
    public const string Document = "doc";
    public const string PhoneVerification = "otp";
    public const string PasswordReset = "pwr";
    public const string Notification = "ntf";

    /// <summary>One attempt to deliver a message on one channel — a row of the delivery log.</summary>
    public const string NotificationDelivery = "ndl";

    /// <summary>A staff send or a broadcast. Not a table: the id groups the deliveries it caused.</summary>
    public const string NotificationBatch = "nbt";
    public const string Package = "pkg";

    /// <summary>A course of appointments booked together. Not a table of its own: the id groups the visits.</summary>
    public const string Series = "ser";

    /// <summary>A reminder rule — <c>trigger + condition + action</c>.</summary>
    public const string Automation = "atm";

    /// <summary>One reminder a rule sent, or claimed to send: the "already sent" record.</summary>
    public const string AutomationDispatch = "ard";

    /// <summary>An asynchronous report export job.</summary>
    public const string ReportExport = "rex";

    /// <summary>A drug or supply in the clinic's catalogue — "ยาและเวชภัณฑ์".</summary>
    public const string InventoryItem = "itm";

    /// <summary>One lot of one item at one branch: a lot number, an expiry date and what is left of it.</summary>
    public const string StockLot = "lot";

    /// <summary>One row of the stock ledger. Append-only.</summary>
    public const string StockMovement = "stm";

    /// <summary>A transfer between branches. Not a table: the id pairs the two ledger rows it wrote per lot.</summary>
    public const string StockTransfer = "trf";

    public const string PurchaseOrder = "po";
    public const string PurchaseOrderLine = "pol";

    public static string New(string prefix)
    {
        Span<byte> buffer = stackalloc byte[16];

        var timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        for (var i = 5; i >= 0; i--)
        {
            buffer[i] = (byte)(timestamp & 0xFF);
            timestamp >>= 8;
        }

        RandomNumberGenerator.Fill(buffer[6..]);

        return string.Concat(prefix, "_", Encode(buffer));
    }

    private static string Encode(ReadOnlySpan<byte> bytes)
    {
        Span<char> chars = stackalloc char[26];
        var bitBuffer = 0;
        var bitCount = 0;
        var index = 0;

        foreach (var b in bytes)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;

            while (bitCount >= 5)
            {
                bitCount -= 5;
                chars[index++] = Alphabet[(bitBuffer >> bitCount) & 0x1F];
            }
        }

        if (bitCount > 0)
        {
            chars[index++] = Alphabet[(bitBuffer << (5 - bitCount)) & 0x1F];
        }

        return new string(chars[..index]);
    }
}
