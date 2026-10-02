using FluentValidation;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.UpdateQueueSettings;

/// <summary>
/// <c>PATCH /clinic/settings/queue</c> — "รูปแบบเลขคิว การรีเซ็ตรายวัน". An absent field is left
/// alone.
/// </summary>
public sealed record UpdateQueueSettingsCommand(
    string? NumberPrefix = null,
    int? NumberDigits = null,
    bool? ResetDaily = null,
    uint? IfMatchVersion = null);

public sealed class UpdateQueueSettingsValidator : AbstractValidator<UpdateQueueSettingsCommand>
{
    public UpdateQueueSettingsValidator()
    {
        RuleFor(command => command.NumberPrefix)
            .Matches("^[A-Za-z]{0,3}$")
            .WithMessage("ตัวอักษรนำหน้าเลขคิวต้องเป็น A–Z ไม่เกิน 3 ตัว");

        RuleFor(command => command.NumberDigits).InclusiveBetween(2, 5);
    }
}

/// <summary>
/// Changes how queue numbers are printed and when they restart.
/// <para>
/// Applies to tickets issued from now on. A ticket already in someone's hand keeps the number it
/// was printed with — renumbering a waiting room mid-morning is how two people come to believe
/// they are both next.
/// </para>
/// <para>
/// The contract gives this setting a <c>PATCH</c> and no <c>GET</c>. An empty body is therefore
/// the read: it changes nothing and returns the settings as they stand.
/// </para>
/// </summary>
public sealed class UpdateQueueSettingsHandler(
    IClinicSettingsStore settings,
    IUnitOfWork unitOfWork,
    RequestValidator validator)
{
    public async Task<QueueSettingsDto> HandleAsync(
        UpdateQueueSettingsCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        if (command is { NumberPrefix: null, NumberDigits: null, ResetDaily: null })
        {
            // Nothing to write, so nothing is written — not even the default row.
            return QueueSettingsDto.From(await settings.QueueAsync(cancellationToken));
        }

        QueueSettings? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var queue = await settings.QueueForUpdateAsync(token);

            ConcurrencyGuard.Ensure(queue, command.IfMatchVersion);

            queue.Update(command.NumberPrefix, command.NumberDigits, command.ResetDaily);

            updated = queue;
        }, cancellationToken);

        return QueueSettingsDto.From(updated!);
    }
}
