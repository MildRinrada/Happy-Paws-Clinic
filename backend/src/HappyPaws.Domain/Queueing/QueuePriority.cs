namespace HappyPaws.Domain.Queueing;

/// <summary>
/// How urgently a ticket must be seen.
/// <para>
/// <b>This is an addition to the API contract, not something read out of it.</b> The reference's
/// queue is strictly first-come with a skip, which means a hit-by-car cannot be put ahead of a nail
/// trim — while the same reference distinguishes appointment types as "นัดปกติ ฉุกเฉิน ติดตามผล
/// ผ่าตัด · กติกาต่างกัน". That distinction dies at check-in, and a clinic that cannot express
/// urgency in the system will express it on paper instead, which is worse: the board then lies.
/// See <c>docs/contract-extensions.md § 1</c>.
/// </para>
/// <para>
/// Three levels, not five. Every extra level is one more judgement call at a busy counter, and the
/// clinically meaningful question is only ever "now, soon, or in turn".
/// </para>
/// </summary>
public enum QueuePriority
{
    /// <summary>In turn. The default, and what every ticket issued without an opinion gets.</summary>
    Normal = 0,

    /// <summary>Ahead of routine work — in pain, very young or very old, deteriorating.</summary>
    Urgent = 1,

    /// <summary>Next, whatever else is waiting. Trauma, collapse, dystocia.</summary>
    Emergency = 2,
}
