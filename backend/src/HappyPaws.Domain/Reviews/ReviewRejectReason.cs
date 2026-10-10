namespace HappyPaws.Domain.Reviews;

/// <summary>
/// The only reasons staff may reject a review (<c>docs/contract-extensions.md § 36</c>).
/// <para>
/// A fixed list on purpose. A low score is not on it: if staff could reject whatever they did not
/// like, the published average would be a figure the clinic chose, not one its customers gave.
/// Rejecting only for these reasons is what keeps the average honest. There is no free-text reason
/// either, because a free-text box is how "ไม่ชอบ" becomes a reason.
/// </para>
/// </summary>
public enum ReviewRejectReason
{
    /// <summary>The text names or identifies someone: a phone number, an address, a staff member's private details.</summary>
    PersonalData,

    /// <summary>Abusive, crude or hateful language.</summary>
    Offensive,

    /// <summary>Not about the clinic's service: another business, politics, an unrelated story.</summary>
    OffTopic,

    /// <summary>Advertising, a link farm, or the same review again.</summary>
    Spam,

    /// <summary>The reviewer asked the clinic to take it down (PDPA). Always honoured.</summary>
    CustomerRequest,
}

/// <summary>The Thai label of each reason, as staff and the audit log read it.</summary>
public static class ReviewRejectReasons
{
    public static string Label(ReviewRejectReason reason) => reason switch
    {
        ReviewRejectReason.PersonalData => "มีข้อมูลส่วนบุคคล",
        ReviewRejectReason.Offensive => "ภาษาไม่สุภาพ",
        ReviewRejectReason.OffTopic => "ไม่เกี่ยวกับบริการของคลินิก",
        ReviewRejectReason.Spam => "สแปมหรือซ้ำ",
        ReviewRejectReason.CustomerRequest => "ลูกค้าขอให้นำออก",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };
}
