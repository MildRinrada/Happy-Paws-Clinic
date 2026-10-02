namespace HappyPaws.Domain.ServiceCatalog;

/// <summary>One heading on the clinic's service menu.</summary>
public sealed record ServiceCategory(string Code, string Name, string NameEn);

/// <summary>
/// The service categories — <c>GET /services/categories</c>, "ตรวจ วัคซีน ทันตกรรม ผ่าตัด แล็บ
/// กรูมมิ่ง".
/// <para>
/// A fixed list, because the contract enumerates exactly six and gives no endpoint that adds a
/// seventh. Making them rows a clinic could edit would need a write endpoint the reference does not
/// have, so they are declared here and a service's <c>category_code</c> is validated against them.
/// </para>
/// </summary>
public static class ServiceCategories
{
    public const string Checkup = "checkup";
    public const string Vaccine = "vaccine";
    public const string Dental = "dental";
    public const string Surgery = "surgery";
    public const string Lab = "lab";
    public const string Grooming = "grooming";

    /// <summary>In the order the contract lists them, which is the order a menu shows them.</summary>
    public static readonly IReadOnlyList<ServiceCategory> All =
    [
        new(Checkup, "ตรวจ", "Check-up"),
        new(Vaccine, "วัคซีน", "Vaccination"),
        new(Dental, "ทันตกรรม", "Dental"),
        new(Surgery, "ผ่าตัด", "Surgery"),
        new(Lab, "แล็บ", "Laboratory"),
        new(Grooming, "กรูมมิ่ง", "Grooming"),
    ];

    public static bool IsKnown(string code) => All.Any(category => category.Code == code);
}
