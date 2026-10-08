using HappyPaws.Application.Clinics;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Application.Notifications.Templates;
using HappyPaws.Domain.Integrations;

namespace HappyPaws.Application.Notifications.ListTemplates;

/// <summary>
/// <c>GET /staff/notifications/templates</c> — "เทมเพลตพร้อมตัวแปร pet_name, vet_name, time".
/// <para>
/// Every template the system sends, with the values it reads, the consent it needs, its wording
/// per channel, and whether it is switched on for LINE. The staff-sendable ones come first, since
/// they are the ones a person at the counter is choosing between.
/// </para>
/// </summary>
public sealed class ListTemplatesHandler(IClinicSettingsStore settings)
{
    public async Task<IReadOnlyList<TemplateDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var line = await settings.ReadAsync(LineChannelSettings.SingletonId, LineChannelSettings.CreateDefault, cancellationToken);

        return
        [
            .. NotificationTemplateCatalogue.All
                .OrderByDescending(template => template.StaffSendable)
                .Select(template => TemplateDto.From(template, line.IsTemplateEnabled(template.Key))),
        ];
    }
}
