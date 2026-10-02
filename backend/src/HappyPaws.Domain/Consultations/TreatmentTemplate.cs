using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Consultations;

/// <summary>
/// A treatment plan the clinic has written down once to reuse —
/// <c>GET /staff/templates/treatments</c>, "เทมเพลตแผนรักษาที่คลินิกทำไว้เอง".
/// <para>
/// It holds what a vet would otherwise type on every such case: the procedures, the medicines, the
/// plan text, and, for a course of visits, how many sessions and how far apart. Applying a template
/// copies it into the case; the case never points back at it, so editing a template later cannot
/// change what a signed record says was done.
/// </para>
/// <para>
/// Prices are not stored here. A procedure that names a service takes that service's price on the
/// day it is applied, so a template written last year does not bill last year's price.
/// </para>
/// </summary>
public sealed class TreatmentTemplate : Entity
{
    public const int MaxItems = 30;

    private readonly List<TemplateProcedure> _procedures = [];
    private readonly List<TemplateMedicine> _medicines = [];

    private TreatmentTemplate()
    {
    }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The species it is written for, or null for any.</summary>
    public string? Species { get; private set; }

    /// <summary>What it is for — the condition or the occasion.</summary>
    public string? Description { get; private set; }

    public IReadOnlyList<TemplateProcedure> Procedures => _procedures;

    public IReadOnlyList<TemplateMedicine> Medicines => _medicines;

    /// <summary>The plan text, as it would go into the case's plan field.</summary>
    public string? Plan { get; private set; }

    /// <summary>For a course of visits, how many. Null for a single visit.</summary>
    public int? Sessions { get; private set; }

    /// <summary>Days between sessions, when <see cref="Sessions"/> is set.</summary>
    public int? SessionIntervalDays { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static TreatmentTemplate Create(
        string name,
        IReadOnlyCollection<TemplateProcedure> procedures,
        IReadOnlyCollection<TemplateMedicine> medicines,
        string? species = null,
        string? description = null,
        string? plan = null,
        int? sessions = null,
        int? sessionIntervalDays = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw Invalid("ต้องระบุชื่อเทมเพลต");
        }

        if (procedures.Count + medicines.Count is 0 or > MaxItems)
        {
            throw Invalid($"เทมเพลตต้องมีหัตถการหรือยารวม 1 ถึง {MaxItems} รายการ");
        }

        if (procedures.Any(item => string.IsNullOrWhiteSpace(item.Name) || item.Quantity <= 0))
        {
            throw Invalid("หัตถการต้องมีชื่อและจำนวนมากกว่าศูนย์");
        }

        if (medicines.Any(item => string.IsNullOrWhiteSpace(item.DrugName) || item.DurationDays <= 0 || item.Quantity <= 0))
        {
            throw Invalid("ยาต้องมีชื่อ จำนวนวัน และจำนวนยามากกว่าศูนย์");
        }

        if ((sessions is null) != (sessionIntervalDays is null) || sessions is < 2 or > 52 || sessionIntervalDays is < 1 or > 365)
        {
            throw Invalid("คอร์สต้องระบุจำนวนครั้ง 2 ถึง 52 ครั้งคู่กับระยะห่าง 1 ถึง 365 วัน");
        }

        var template = new TreatmentTemplate
        {
            Id = EntityId.New(EntityId.TreatmentTemplate),
            Name = name.Trim(),
            Species = Trimmed(species),
            Description = Trimmed(description),
            Plan = Trimmed(plan),
            Sessions = sessions,
            SessionIntervalDays = sessionIntervalDays,
        };

        template._procedures.AddRange(procedures);
        template._medicines.AddRange(medicines);

        return template;
    }

    private static DomainException Invalid(string message) => new("invalid_treatment_template", message);

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>A procedure in a template. <paramref name="ServiceId"/> is where its price comes from.</summary>
public sealed record TemplateProcedure(string Name, int Quantity = 1, string? ServiceId = null, string? Note = null);

/// <summary>A medicine in a template, written the way a prescription is.</summary>
public sealed record TemplateMedicine(
    string DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    int Quantity,
    string? Instructions = null);
