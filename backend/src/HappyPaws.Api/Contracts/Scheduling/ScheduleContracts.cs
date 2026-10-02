namespace HappyPaws.Api.Contracts.Scheduling;

/// <summary>
/// <c>POST /staff/schedules/shifts</c>. One shift, or with <c>rrule</c> the same shift repeated,
/// e.g. <c>FREQ=WEEKLY;BYDAY=MO,TU,TH,FR;UNTIL=20261231</c>. A break splits each shift in two.
/// </summary>
public sealed record CreateShiftsRequest(
    string StaffId,
    string BranchId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    ShiftBreakRequest? Break = null,
    string? Rrule = null);

/// <summary>A break inside the first shift; repeated with it.</summary>
public sealed record ShiftBreakRequest(DateTimeOffset StartsAt, DateTimeOffset EndsAt);

/// <summary><c>POST /staff/schedules/time-off</c>.</summary>
public sealed record RequestTimeOffRequest(string StaffId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Reason = null);

/// <summary><c>PUT /staff/schedules/assignment-rules</c>. The whole set, in the order rules are tried.</summary>
public sealed record ReplaceAssignmentRulesRequest(IReadOnlyList<AssignmentRuleRequest>? Rules);

/// <summary>One rule: cases for this species and/or service go to these staff first.</summary>
public sealed record AssignmentRuleRequest(string? Species, string? ServiceId, IReadOnlyList<string>? StaffIds);
