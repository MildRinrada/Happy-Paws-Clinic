namespace HappyPaws.Application.Notifications.Templates;

/// <summary>One value a template fills in — <c>pet_name</c>, <c>time</c>.</summary>
/// <param name="Name">The key in <see cref="NotificationRequest.Data"/>.</param>
/// <param name="Required">A staff send without it is refused. Optional ones drop the words around them.</param>
/// <param name="MaxLength">The longest value a staff send may give.</param>
public sealed record TemplateVariable(string Name, bool Required = true, int MaxLength = 100);

/// <summary>
/// A message in words, once per channel.
/// <para>
/// Text uses <c>{name}</c> for a value and <c>[ ... ]</c> around words that only make sense when
/// every value inside them is present: <c>เชิญที่ห้องตรวจ[ {room}]</c> reads "เชิญที่ห้องตรวจ"
/// when no room was given. Two values are always available without being passed:
/// <c>{clinic}</c>, the clinic's name, and <c>{portal_url}</c>.
/// </para>
/// <para>
/// The SMS wording is kept to one Thai part (70 characters) for the short notices, because a
/// two-part SMS costs two credits and arrives as two messages on some phones. LINE and email have
/// room, and say more.
/// </para>
/// </summary>
/// <param name="Key">The template key use cases pass.</param>
/// <param name="Name">What staff see in the template list.</param>
/// <param name="StaffSendable">True when <c>POST /staff/notifications/send</c> may send it.</param>
/// <param name="Variables">The values it reads.</param>
/// <param name="Title">The inbox title and email subject.</param>
/// <param name="Line">The LINE text, also the inbox body and the email body.</param>
/// <param name="Sms">The SMS text.</param>
public sealed record NotificationTemplate(
    string Key,
    string Name,
    bool StaffSendable,
    IReadOnlyList<TemplateVariable> Variables,
    string Title,
    string Line,
    string Sms);
