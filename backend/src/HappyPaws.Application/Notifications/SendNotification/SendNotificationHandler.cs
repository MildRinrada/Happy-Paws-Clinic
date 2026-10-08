using FluentValidation;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Notifications.Delivery;
using HappyPaws.Application.Notifications.Dtos;
using HappyPaws.Application.Notifications.Templates;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace HappyPaws.Application.Notifications.SendNotification;

/// <summary>
/// <c>POST /staff/notifications/send</c> — "ส่งเดี่ยวหรือเป็นกลุ่ม · เลือก channel ได้".
/// <c>Channel</c> is <c>auto</c> (default: LINE, then SMS, then email), or one of <c>line</c>,
/// <c>sms</c>, <c>email</c> to use that one only. <c>Variables</c> fill the template.
/// </summary>
public sealed record SendNotificationCommand(
    string Template,
    IReadOnlyList<string> CustomerIds,
    string? Channel = null,
    IReadOnlyDictionary<string, string>? Variables = null,
    string? BranchId = null);

public sealed class SendNotificationValidator : AbstractValidator<SendNotificationCommand>
{
    public const int MaxRecipients = 100;

    public static readonly IReadOnlyList<string> Channels = ["auto", "line", "sms", "email"];

    public SendNotificationValidator()
    {
        RuleFor(command => command.Template)
            .Must(key => NotificationTemplateCatalogue.Find(key) is { StaffSendable: true })
            .WithMessage(_ =>
                "template ต้องเป็นหนึ่งใน: "
                + string.Join(", ", NotificationTemplateCatalogue.All.Where(t => t.StaffSendable).Select(t => t.Key)));

        RuleFor(command => command.CustomerIds)
            .NotEmpty().WithMessage("ต้องเลือกลูกค้าอย่างน้อยหนึ่งคน")
            .Must(ids => ids is null || ids.Count <= MaxRecipients)
            .WithMessage($"ส่งได้ไม่เกิน {MaxRecipients} คนต่อครั้ง");

        RuleForEach(command => command.CustomerIds).NotEmpty().MaximumLength(40);

        RuleFor(command => command.Channel)
            .Must(channel => channel is null || Channels.Contains(channel))
            .WithMessage($"channel ต้องเป็น {string.Join(", ", Channels)}");

        RuleFor(command => command.Variables)
            .Custom((variables, context) =>
            {
                if (NotificationTemplateCatalogue.Find(context.InstanceToValidate.Template) is not { StaffSendable: true } template)
                {
                    return;
                }

                variables ??= new Dictionary<string, string>();

                foreach (var variable in template.Variables)
                {
                    variables.TryGetValue(variable.Name, out var value);

                    if (variable.Required && string.IsNullOrWhiteSpace(value))
                    {
                        context.AddFailure($"variables.{variable.Name}", $"ต้องระบุ {variable.Name}");
                    }
                    else if (value is { } given && given.Trim().Length > variable.MaxLength)
                    {
                        context.AddFailure($"variables.{variable.Name}", $"{variable.Name} ยาวได้ไม่เกิน {variable.MaxLength} ตัวอักษร");
                    }
                }

                foreach (var unknown in variables.Keys.Where(key => template.Variables.All(variable => variable.Name != key)))
                {
                    context.AddFailure($"variables.{unknown}", $"เทมเพลตนี้ไม่มีตัวแปร {unknown}");
                }
            });
    }
}

/// <summary>
/// Sends one template to one or more customers, on behalf of a member of staff.
/// <para>
/// <b>The same road as every other message.</b> Each customer's copy goes through
/// <see cref="INotificationDispatcher"/>, so the consent gate, the web inbox and the delivery log
/// apply exactly as they do to a queue call. A staff send cannot reach someone who opted out; it is
/// recorded as skipped instead, and the answer says so.
/// </para>
/// <para>
/// Only the templates marked sendable can be used. A receipt or a queue call names a real record,
/// and sending one by hand would tell an owner they paid, or were called, when they were not.
/// </para>
/// <para>
/// The answer is read back from the delivery log by batch id rather than collected from the
/// dispatcher, so it reports what was actually recorded, the same rows the log page shows.
/// </para>
/// </summary>
public sealed class SendNotificationHandler(
    INotificationDispatcher dispatcher,
    ICustomerRepository customers,
    IBranchRepository branches,
    IDeliveryLog deliveryLog,
    BranchGuard branchGuard,
    ICurrentUser currentUser,
    RequestValidator validator,
    ILogger<SendNotificationHandler> logger)
{
    public async Task<SendResultDto> HandleAsync(
        SendNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        var branchId = await BranchForAsync(command.BranchId, cancellationToken);
        var customerIds = command.CustomerIds.Select(id => id.Trim()).Distinct(StringComparer.Ordinal).ToList();

        await EnsureCustomersExistAsync(customers, customerIds, cancellationToken);

        var batchId = EntityId.New(EntityId.NotificationBatch);
        var data = (command.Variables ?? new Dictionary<string, string>())
            .ToDictionary(pair => pair.Key, pair => pair.Value.Trim(), StringComparer.Ordinal);
        var channel = command.Channel is null or "auto" ? null : command.Channel;

        foreach (var customerId in customerIds)
        {
            try
            {
                await dispatcher.SendAsync(
                    new NotificationRequest(customerId, command.Template, data)
                    {
                        BranchId = branchId,
                        Channel = channel,
                        Source = DeliverySource.Manual,
                        BatchId = batchId,
                        SentByStaffId = staffId,
                    },
                    cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // One customer's failure is that customer's line in the answer, not the whole send's.
                logger.LogError(exception, "Staff send {BatchId} failed for customer {CustomerId}", batchId, customerId);
            }
        }

        return SendResultDto.Summarise(batchId, customerIds, await deliveryLog.ForBatchAsync(batchId, cancellationToken));
    }

    /// <summary>Every id must be a live customer: a typo answers 404 before anything is sent.</summary>
    internal static async Task EnsureCustomersExistAsync(
        ICustomerRepository customers,
        IReadOnlyList<string> customerIds,
        CancellationToken cancellationToken)
    {
        foreach (var customerId in customerIds)
        {
            var customer = await customers.FindByIdAsync(customerId, cancellationToken);

            if (customer is null || customer.IsMerged)
            {
                throw new NotFoundException("customer", customerId);
            }
        }
    }

    private async Task<string?> BranchForAsync(string? requested, CancellationToken cancellationToken)
    {
        if (requested is { Length: > 0 } branchId)
        {
            if ((await branches.MissingAsync([branchId], cancellationToken)).Count > 0)
            {
                throw new NotFoundException("branch", branchId);
            }

            branchGuard.EnsureCanAccess(branchId);
            return branchId;
        }

        // A branch-bound sender's message belongs to their branch, so it shows in their own log.
        return currentUser.BranchIds.Count == 1 ? currentUser.BranchIds.First() : null;
    }
}
