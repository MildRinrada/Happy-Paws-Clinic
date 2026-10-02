using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Clinics;

/// <summary>
/// How queue numbers look and when they start again — <c>PATCH /clinic/settings/queue</c>,
/// "รูปแบบเลขคิว การรีเซ็ตรายวัน".
/// <para>
/// The defaults reproduce the contract's own example, <c>tkt_A014</c>: a letter, three digits, and
/// a fresh sequence every morning ("คิวรีเซ็ตรายวันต่อสาขา").
/// </para>
/// </summary>
public sealed class QueueSettings : Entity
{
    public const string SingletonId = "set_queue";

    private QueueSettings()
    {
    }

    /// <summary>Printed in front of the number. May be empty for a clinic that wants bare digits.</summary>
    public string NumberPrefix { get; private set; } = "A";

    /// <summary>Minimum digits, zero-padded. A busy day simply grows past it.</summary>
    public int NumberDigits { get; private set; } = 3;

    /// <summary>
    /// True restarts numbering at 1 each day per branch. False lets the sequence run on, which a
    /// small clinic may prefer so that a number names one visit for good.
    /// </summary>
    public bool ResetDaily { get; private set; } = true;

    public static QueueSettings CreateDefault() => new() { Id = SingletonId };

    /// <summary>The number as it is printed and called out — <c>A014</c>.</summary>
    public string Format(int sequence) =>
        string.Concat(NumberPrefix, sequence.ToString($"D{NumberDigits}"));

    /// <summary>Applies a partial update; a null argument leaves that setting alone.</summary>
    public void Update(string? numberPrefix = null, int? numberDigits = null, bool? resetDaily = null)
    {
        if (numberPrefix is not null)
        {
            var prefix = numberPrefix.Trim().ToUpperInvariant();

            // Letters only and short: this is read aloud over a waiting room, and it has to fit on
            // a thermal ticket next to the digits.
            if (prefix.Length > 3 || prefix.Any(c => c is < 'A' or > 'Z'))
            {
                throw new DomainException("invalid_queue_settings", "ตัวอักษรนำหน้าเลขคิวต้องเป็น A–Z ไม่เกิน 3 ตัว");
            }

            NumberPrefix = prefix;
        }

        if (numberDigits is { } digits)
        {
            if (digits is < 2 or > 5)
            {
                throw new DomainException("invalid_queue_settings", "จำนวนหลักของเลขคิวต้องอยู่ระหว่าง 2–5");
            }

            NumberDigits = digits;
        }

        ResetDaily = resetDaily ?? ResetDaily;
    }
}
