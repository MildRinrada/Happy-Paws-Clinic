using HappyPaws.Application.Common.Authorization;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Common.Pagination;
using HappyPaws.Application.Messaging.Dtos;

namespace HappyPaws.Application.Messaging.ListThreads;

/// <summary>
/// <c>GET /staff/threads?status=&amp;customer_id=&amp;thread_id=&amp;messages=&amp;limit=&amp;cursor=</c>.
/// </summary>
/// <param name="Status"><c>awaiting_reply</c> or <c>replied</c>; absent lists both.</param>
/// <param name="CustomerId">One customer's thread.</param>
/// <param name="ThreadId">One thread, to open it.</param>
/// <param name="Messages">How many of each thread's newest messages to include, 0 to 100. Default 20.</param>
/// <param name="Page">Paging.</param>
public sealed record ListThreadsQuery(
    string? Status = null,
    string? CustomerId = null,
    string? ThreadId = null,
    int? Messages = null,
    CursorPageRequest? Page = null);

/// <summary>
/// "กล่องข้อความรวมทุกช่องทางต่อลูกค้าหนึ่งคน": the customers' conversations, those awaiting a reply
/// first, then the most recently active.
/// <para>
/// <b>Branches.</b> A thread is filed under the branch the customer last booked at. A branch-bound
/// reader sees their branches' threads and those with no branch (a customer who never booked), the
/// same rule as the delivery log. Unrestricted readers see every thread.
/// </para>
/// <para>
/// <b>One endpoint for the list and the conversation.</b> The contract has a single read, so the
/// list carries each thread's newest messages; <c>?messages=0</c> keeps the list light, and
/// <c>?thread_id=&amp;messages=100</c> opens one conversation. Reading does not mark anything read:
/// a thread stops awaiting a reply when someone answers it.
/// </para>
/// <para>
/// Not audited, like opening a customer record: a thread holds what the owner chose to write to the
/// clinic, not the clinical file (<c>AuditActions</c> draws the line at health data).
/// </para>
/// </summary>
public sealed class ListThreadsHandler(IMessageThreadRepository threads, BranchGuard branchGuard)
{
    public const int DefaultMessages = 20;
    public const int MaxMessages = 100;

    public async Task<CursorPage<ThreadDto>> HandleAsync(ListThreadsQuery query, CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        bool? awaiting = query.Status switch
        {
            null or "" => null,
            ThreadDto.AwaitingReply => true,
            ThreadDto.Replied => false,
            _ => null,
        };

        if (query.Status is { Length: > 0 } status && status is not (ThreadDto.AwaitingReply or ThreadDto.Replied))
        {
            errors["status"] = [$"status ต้องเป็น {ThreadDto.AwaitingReply} หรือ {ThreadDto.Replied}"];
        }

        var perThread = query.Messages ?? DefaultMessages;

        if (perThread is < 0 or > MaxMessages)
        {
            errors["messages"] = [$"messages ต้องอยู่ระหว่าง 0 ถึง {MaxMessages}"];
        }

        if (query.CustomerId is { Length: > 40 })
        {
            errors["customer_id"] = ["customer_id ยาวเกินไป"];
        }

        if (query.ThreadId is { Length: > 40 })
        {
            errors["thread_id"] = ["thread_id ยาวเกินไป"];
        }

        if (errors.Count > 0)
        {
            throw new ValidationFailedException(errors);
        }

        var page = await threads.ListAsync(
            new ThreadQuery(
                branchGuard.VisibleBranchIds,
                awaiting,
                NullIfBlank(query.CustomerId),
                NullIfBlank(query.ThreadId)),
            query.Page ?? new CursorPageRequest(),
            cancellationToken);

        var data = new List<ThreadDto>(page.Data.Count);

        foreach (var row in page.Data)
        {
            var messages = perThread == 0
                ? []
                : await threads.RecentMessagesAsync(row.Thread.Id, perThread, cancellationToken);

            data.Add(ThreadDto.From(row, messages));
        }

        return new CursorPage<ThreadDto>(data, page.NextCursor);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
