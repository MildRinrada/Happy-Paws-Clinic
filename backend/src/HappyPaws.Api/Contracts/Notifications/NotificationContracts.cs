namespace HappyPaws.Api.Contracts.Notifications;

/// <summary>
/// <c>POST /staff/notifications/send</c> — "ส่งเดี่ยวหรือเป็นกลุ่ม · เลือก channel ได้".
/// <para>
/// <c>template</c> is a staff-sendable key from <c>GET /staff/notifications/templates</c>;
/// <c>variables</c> fills it (<c>pet_name</c>, <c>vet_name</c>, <c>time</c>). <c>channel</c> is
/// <c>auto</c> (default), <c>line</c>, <c>sms</c> or <c>email</c>. <c>branch_id</c> files the send
/// under a branch for the log; without it a branch-bound sender's own branch is used.
/// </para>
/// </summary>
public sealed record SendNotificationRequest(
    string Template,
    IReadOnlyList<string>? CustomerIds = null,
    string? Channel = null,
    Dictionary<string, string>? Variables = null,
    string? BranchId = null);

/// <summary>
/// <c>POST /staff/integrations/line/broadcast</c> — "ประกาศถึงกลุ่มลูกค้าที่คัดไว้ · จำกัดโควตา".
/// The group is the list of customers given; see <c>docs/features/line.md</c> on saved segments.
/// </summary>
public sealed record LineBroadcastRequest(IReadOnlyList<string>? CustomerIds = null, string? Message = null, string? SegmentId = null);

/// <summary>
/// <c>PUT /staff/integrations/line</c> — "channel id/secret · เปิดปิดชนิดการแจ้งเตือน". Every field
/// is optional and an absent one is unchanged; an empty string clears a stored credential; an
/// empty body reads the settings back. <c>notification_types</c> maps a template key to on or off.
/// </summary>
public sealed record ConfigureLineRequest(
    string? ChannelId = null,
    string? ChannelSecret = null,
    string? ChannelAccessToken = null,
    bool? Enabled = null,
    Dictionary<string, bool>? NotificationTypes = null,
    int? BroadcastMonthlyLimit = null);
