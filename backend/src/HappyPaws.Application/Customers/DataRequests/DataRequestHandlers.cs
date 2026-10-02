using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Customers.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Customers;

namespace HappyPaws.Application.Customers.DataRequests;

/// <summary><c>POST /portal/data-requests</c> — a customer exercises a data-protection right.</summary>
public sealed record RaiseDataRequestCommand(string CustomerId, string Type, string? Detail = null);

/// <summary><c>GET /staff/data-requests?status=</c> — the clinic's queue of outstanding requests.</summary>
public sealed record ListDataRequestsQuery(string? Status = null, string? CustomerId = null);

/// <summary><c>POST /staff/data-requests/{id}/close</c> — what the clinic did, or why it declined.</summary>
public sealed record CloseDataRequestCommand(
    string RequestId,
    string Outcome,
    string Resolution,
    uint? IfMatchVersion = null);

/// <summary>
/// Receives a request and starts its clock.
/// <para>
/// The clock is the substance. Every data-protection regime this clinic operates under measures
/// compliance in days from receipt, and a request made by phone to a receptionist has no receipt
/// at all — which is the situation the contract leaves in place today.
/// </para>
/// </summary>
public sealed class RaiseDataRequestHandler(
    IDataSubjectRequestRepository requests,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<DataRequestDto> HandleAsync(
        RaiseDataRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var type = Parse<DataRequestType>(command.Type, "type", "access, correction, erasure, portability");
        DataSubjectRequest? raised = null;

        await unitOfWork.ExecuteInTransactionAsync(token =>
        {
            var request = DataSubjectRequest.Raise(command.CustomerId, type, clock.UtcNow, command.Detail);

            requests.Add(request);

            audit.Record(new AuditEvent(
                AuditActions.DataRequestRaised,
                AuditResources.DataRequest,
                request.Id,
                Details: new Dictionary<string, object?>
                {
                    ["customer_id"] = command.CustomerId,
                    ["type"] = type.ToString().ToLowerInvariant(),
                }));

            raised = request;

            return Task.CompletedTask;
        }, cancellationToken);

        return DataRequestDto.From(raised!, clock.UtcNow);
    }

    internal static TEnum Parse<TEnum>(string value, string field, string allowed)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ValidationFailedException(new Dictionary<string, string[]>
            {
                [field] = [$"ต้องเป็นค่าใดค่าหนึ่งใน: {allowed}"],
            });
}

/// <summary>
/// The clinic's worklist, oldest first — the opposite of every other list here, because the oldest
/// request is the one closest to a statutory deadline.
/// </summary>
public sealed class ListDataRequestsHandler(IDataSubjectRequestRepository requests, IClock clock)
{
    public async Task<IReadOnlyList<DataRequestDto>> HandleAsync(
        ListDataRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        DataRequestStatus? status = query.Status is { Length: > 0 } value
            ? RaiseDataRequestHandler.Parse<DataRequestStatus>(value, "status", "open, completed, refused")
            : null;

        var rows = await requests.ListAsync(status, query.CustomerId, cancellationToken);

        return [.. rows.Select(request => DataRequestDto.From(request, clock.UtcNow))];
    }
}

/// <summary>
/// Closes a request with a stated outcome.
/// <para>
/// Refusing is a first-class outcome rather than an error path. An erasure request against a signed
/// medical record will often be refused — the record is evidence of treatment and this system never
/// deletes one — and the lawful thing to do is answer, in writing, with the reason. A system that
/// only modelled "completed" would push that answer somewhere it cannot be found again.
/// </para>
/// </summary>
public sealed class CloseDataRequestHandler(
    IDataSubjectRequestRepository requests,
    IAuditTrail audit,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<DataRequestDto> HandleAsync(
        CloseDataRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new ForbiddenException("caller is not authenticated");
        var outcome = RaiseDataRequestHandler.Parse<DataRequestStatus>(
            command.Outcome, "outcome", "completed, refused");

        DataSubjectRequest? closed = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var request = await requests.FindByIdAsync(command.RequestId, token)
                ?? throw new NotFoundException("data request", command.RequestId);

            ConcurrencyGuard.Ensure(request, command.IfMatchVersion);

            request.Close(outcome, userId, command.Resolution, clock.UtcNow);

            audit.Record(new AuditEvent(
                AuditActions.DataRequestClosed,
                AuditResources.DataRequest,
                request.Id,
                Details: new Dictionary<string, object?>
                {
                    ["customer_id"] = request.CustomerId,
                    ["outcome"] = outcome.ToString().ToLowerInvariant(),
                    ["resolution"] = command.Resolution,
                    ["open_days"] = (int)request.AgeAt(clock.UtcNow).TotalDays,
                }));

            closed = request;
        }, cancellationToken);

        return DataRequestDto.From(closed!, clock.UtcNow);
    }
}
