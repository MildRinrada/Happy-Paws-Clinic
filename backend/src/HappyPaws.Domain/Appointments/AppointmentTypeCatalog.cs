using System.Text.RegularExpressions;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Queueing;

namespace HappyPaws.Domain.Appointments;

/// <summary>
/// One kind of appointment and the rules that come with it — "นัดปกติ ฉุกเฉิน ติดตามผล ผ่าตัด ·
/// กติกาต่างกัน".
/// </summary>
/// <param name="Code">What an appointment stores — <c>regular</c>, <c>emergency</c>.</param>
/// <param name="Name">What people read.</param>
/// <param name="DurationMinutes">Overrides the service's length, or null to use it.</param>
/// <param name="OnlineBooking">Whether customers may book this type themselves.</param>
/// <param name="LeadTimeMinutes">The notice a customer must give, or null for the clinic's default.</param>
/// <param name="QueuePriority">The priority the queue ticket starts with at check-in.</param>
public sealed record AppointmentType(
    string Code,
    string Name,
    int? DurationMinutes = null,
    bool OnlineBooking = true,
    int? LeadTimeMinutes = null,
    QueuePriority QueuePriority = QueuePriority.Normal);

/// <summary>
/// The clinic's appointment types — <c>PUT /staff/appointment-types</c>.
/// <para>
/// One row, replaced whole, like the assignment rules. The four the contract names are the
/// defaults. Each type carries four rules, and each is enforced somewhere rather than stored: the
/// customer booking path checks <see cref="AppointmentType.OnlineBooking"/> and the lead time,
/// every booking path takes the duration, and check-in starts the queue ticket at the type's
/// priority. That is what "กติกาต่างกัน" comes to: an emergency cannot be booked from home, and
/// when one arrives it goes to the front.
/// </para>
/// <para>
/// <c>regular</c> must always exist. It is what every booking that does not name a type is, and
/// what every appointment made before types existed already says.
/// </para>
/// </summary>
public sealed partial class AppointmentTypeCatalog : Entity
{
    public const string SingletonId = "set_appointment_types";
    public const string DefaultCode = "regular";
    public const int MaxTypes = 20;

    private readonly List<AppointmentType> _types = [];

    private AppointmentTypeCatalog()
    {
    }

    public IReadOnlyList<AppointmentType> Types => _types;

    public static AppointmentTypeCatalog CreateDefault()
    {
        var catalog = new AppointmentTypeCatalog { Id = SingletonId };

        catalog._types.AddRange(
        [
            new AppointmentType(DefaultCode, "นัดปกติ"),
            new AppointmentType("emergency", "ฉุกเฉิน", OnlineBooking: false, QueuePriority: QueuePriority.Emergency),
            new AppointmentType("follow_up", "ติดตามผล"),
            new AppointmentType("surgery", "ผ่าตัด", OnlineBooking: false),
        ]);

        return catalog;
    }

    /// <summary>The type with this code, or null.</summary>
    public AppointmentType? Find(string? code) =>
        _types.FirstOrDefault(type => type.Code == (string.IsNullOrWhiteSpace(code) ? DefaultCode : code.Trim()));

    public void Replace(IReadOnlyCollection<AppointmentType> types)
    {
        if (types.Count is 0 or > MaxTypes)
        {
            throw Invalid($"ต้องมีประเภทนัด 1 ถึง {MaxTypes} ประเภท");
        }

        var cleaned = types.Select(type => type with { Code = type.Code.Trim(), Name = type.Name.Trim() }).ToList();

        foreach (var type in cleaned)
        {
            if (!CodePattern().IsMatch(type.Code))
            {
                throw Invalid($"รหัสประเภทนัด '{type.Code}' ต้องเป็นตัวพิมพ์เล็ก ตัวเลข หรือ _ ยาว 2 ถึง 30 ตัว");
            }

            if (type.Name.Length is 0 or > 60)
            {
                throw Invalid($"ชื่อประเภทนัด {type.Code} ต้องยาว 1 ถึง 60 ตัวอักษร");
            }

            if (type.DurationMinutes is { } minutes && (minutes is < 5 or > 480 || minutes % 5 != 0))
            {
                throw Invalid($"ระยะเวลาของ {type.Code} ต้องเป็นจำนวนเท่าของ 5 ระหว่าง 5 ถึง 480 นาที");
            }

            if (type.LeadTimeMinutes is < 0 or > 10_080)
            {
                throw Invalid($"เวลาจองล่วงหน้าของ {type.Code} ต้องอยู่ระหว่าง 0 ถึง 7 วัน");
            }

            if (!Enum.IsDefined(type.QueuePriority))
            {
                throw Invalid($"ลำดับความเร่งด่วนของ {type.Code} ไม่ถูกต้อง");
            }
        }

        if (cleaned.Select(type => type.Code).Distinct(StringComparer.Ordinal).Count() != cleaned.Count)
        {
            throw Invalid("รหัสประเภทนัดซ้ำกัน");
        }

        if (cleaned.All(type => type.Code != DefaultCode))
        {
            throw Invalid($"ต้องมีประเภท {DefaultCode} เสมอ เพราะเป็นประเภทของนัดที่ไม่ได้ระบุประเภท");
        }

        _types.Clear();
        _types.AddRange(cleaned);
    }

    private static DomainException Invalid(string message) => new("invalid_appointment_types", message);

    [GeneratedRegex("^[a-z][a-z0-9_]{1,29}$")]
    private static partial Regex CodePattern();
}
