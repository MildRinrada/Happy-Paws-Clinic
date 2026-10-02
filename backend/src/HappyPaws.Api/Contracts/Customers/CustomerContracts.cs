namespace HappyPaws.Api.Contracts.Customers;

/// <summary>
/// <c>POST /staff/customers</c> — "สร้างจากเคาน์เตอร์ · ตรวจซ้ำด้วยเบอร์โทร".
/// <para>
/// <c>allow_duplicate_phone</c> is sent only after a first attempt was refused with
/// <c>409 duplicate_customer</c>, whose <c>details.existing</c> lists the records already on that
/// number. Same shape as the allergy check on prescriptions, for the same reason: the system knows
/// something is odd and the person at the counter knows whether it matters.
/// </para>
/// </summary>
public sealed record CreateCustomerRequest(
    string DisplayName,
    string Phone,
    string? Email = null,
    bool AllowDuplicatePhone = false);

/// <summary>
/// <c>POST /staff/customers/{id}/merge</c> — "รวมเรกคอร์ดซ้ำ · ย้ายสัตว์และประวัติทั้งหมด".
/// <para>
/// The <b>path id survives</b>; this names the duplicate it absorbs. The contract does not say
/// which way round it goes — see <c>docs/assumptions.md § 12</c>.
/// </para>
/// </summary>
public sealed record MergeCustomerRequest(string DuplicateCustomerId);
