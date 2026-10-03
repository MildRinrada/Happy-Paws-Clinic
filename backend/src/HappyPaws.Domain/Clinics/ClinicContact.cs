using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Clinics;

/// <summary>
/// One way to reach the clinic — a phone number, an email address, a LINE account, a map. <b>Not in
/// the API reference</b>; see <c>docs/contract-extensions.md § 15</c>.
/// <para>
/// The contract's clinic profile is name, logo, colour, licence and operator, and its branches carry
/// an address and coordinates. Nothing holds the phone number a customer actually calls, so the
/// public site had it typed into its own source, twice, and the two copies had already drifted.
/// </para>
/// <para>
/// A list of entries rather than fixed fields, because clinics differ in what they publish: one has a
/// hotline and no Instagram, another two phone lines. <see cref="Kind"/> says what an entry is, so a
/// page can draw a phone as a call link and a map as a map; <see cref="Label"/> is what a person
/// reads beside it. An entry belongs to one branch, or with no branch to the whole clinic.
/// </para>
/// </summary>
public sealed class ClinicContact : Entity
{
    public const int LabelMax = 100;
    public const int ValueMax = 500;
    public const int UrlMax = 1000;

    /// <summary>The one embed address a map may carry: Google Maps' own iframe source.</summary>
    public const string MapEmbedPrefix = "https://www.google.com/maps/embed";

    private ClinicContact()
    {
    }

    public string Kind { get; private set; } = string.Empty;

    /// <summary>What a person reads beside the value — "โทรศัพท์คลินิก", "สายด่วนฉุกเฉิน".</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>What is shown — "02-123-4567", "@happypaws", an address line for a map.</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>Where the entry links to, when that is not obvious from the value: a profile page, a map link.</summary>
    public string? Url { get; private set; }

    /// <summary>A map's Google Maps embed address, drawn as a live map. Maps only.</summary>
    public string? EmbedUrl { get; private set; }

    /// <summary>The branch this entry is for. Null is the whole clinic.</summary>
    public string? BranchId { get; private set; }

    /// <summary>Smaller first. Ties fall back to the order entries were added.</summary>
    public int SortOrder { get; private set; }

    public static ClinicContact Add(
        string kind,
        string label,
        string value,
        string? url,
        string? embedUrl,
        string? branchId,
        int sortOrder)
    {
        var contact = new ClinicContact { Id = EntityId.New(EntityId.Contact) };

        contact.Apply(kind, label, value, url, embedUrl, branchId, sortOrder);

        return contact;
    }

    /// <summary>Replaces every field at once. The caller fills in the ones it was not asked to change.</summary>
    public void Change(
        string kind,
        string label,
        string value,
        string? url,
        string? embedUrl,
        string? branchId,
        int sortOrder) =>
        Apply(kind, label, value, url, embedUrl, branchId, sortOrder);

    private void Apply(
        string kind,
        string label,
        string value,
        string? url,
        string? embedUrl,
        string? branchId,
        int sortOrder)
    {
        var normalisedKind = kind?.Trim().ToLowerInvariant() ?? string.Empty;

        if (!ContactKinds.All.Contains(normalisedKind))
        {
            throw new DomainException("invalid_contact", "ประเภทช่องทางติดต่อไม่ถูกต้อง");
        }

        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("invalid_contact", "ต้องระบุชื่อที่แสดงและข้อมูลติดต่อ");
        }

        var link = Blank(url);
        var embed = Blank(embedUrl);

        // Only a web address is accepted as a link. A tel: or mailto: is derived from the value by
        // whoever draws it, and anything else (javascript:, data:) has no business on a public page.
        if (link is not null && !IsWebAddress(link))
        {
            throw new DomainException("invalid_contact", "ลิงก์ต้องขึ้นต้นด้วย http:// หรือ https://");
        }

        if (embed is not null && (normalisedKind != ContactKinds.Map || !embed.StartsWith(MapEmbedPrefix, StringComparison.Ordinal)))
        {
            throw new DomainException("invalid_contact", "ลิงก์ฝังแผนที่ใช้ได้กับแผนที่ และต้องเป็นลิงก์ embed ของ Google Maps");
        }

        Kind = normalisedKind;
        Label = label.Trim();
        Value = value.Trim();
        Url = link;
        EmbedUrl = embed;
        BranchId = Blank(branchId);
        SortOrder = sortOrder;
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsWebAddress(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

/// <summary>What a contact entry can be. Each is drawn its own way on the public site.</summary>
public static class ContactKinds
{
    public const string Phone = "phone";
    public const string Emergency = "emergency";
    public const string Email = "email";
    public const string Line = "line";
    public const string Facebook = "facebook";
    public const string Instagram = "instagram";
    public const string Website = "website";
    public const string Map = "map";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Phone, Emergency, Email, Line, Facebook, Instagram, Website, Map, Other,
    };
}
