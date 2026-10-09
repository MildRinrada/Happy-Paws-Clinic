using FluentValidation;
using HappyPaws.Application.Analytics;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Application.Reporting.Dtos;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Reporting;

namespace HappyPaws.Application.Reporting.StartExport;

/// <summary><c>POST /staff/reports/exports</c>.</summary>
/// <param name="Report"><c>revenue</c>, <c>services</c>, <c>retention</c>, <c>staff_performance</c>, <c>appointments</c>, <c>queue</c> or <c>workload</c>.</param>
/// <param name="Format"><c>csv</c> (the default), <c>xlsx</c> or <c>pdf</c>. The last two are not in the API reference; see <c>docs/contract-extensions.md § 21</c>.</param>
/// <param name="From">First clinic day; the report's own default span back from <paramref name="To"/> when absent.</param>
/// <param name="To">Last clinic day; today when absent.</param>
/// <param name="BranchId">One branch; every branch the caller may see when absent.</param>
public sealed record StartExportCommand(
    string? Report,
    string? Format = null,
    DateOnly? From = null,
    DateOnly? To = null,
    string? BranchId = null);

public sealed class StartExportValidator : AbstractValidator<StartExportCommand>
{
    public StartExportValidator()
    {
        RuleFor(command => command.Report)
            .Must(report => WireEnum.TryParse<ReportKind>(report, out _))
            .WithMessage($"report ต้องเป็น {WireEnum.Names<ReportKind>()}");

        RuleFor(command => command.Format)
            .Must(format => format is null || WireEnum.TryParse<ReportFormat>(format, out _))
            .WithMessage($"format ต้องเป็น {WireEnum.Names<ReportFormat>()}");

        RuleFor(command => command.BranchId).MaximumLength(40);
    }
}

/// <summary>
/// "งานส่งออกแบบ async · คืน job id" — queues an export and answers at once.
/// <para>
/// Everything that depends on who is asking is settled here, while there is a caller: the scope the
/// report itself needs (<see cref="ReportCatalogue"/>), the span, and the branches, resolved by the
/// same <see cref="ReportWindows"/> the dashboard uses so an export of "this month" covers exactly
/// what the screen showed. The worker only builds what was stored.
/// </para>
/// <para>
/// Audited when asked for rather than when downloaded: the download is a signed storage URL, and
/// the decision to take the data out is this request.
/// </para>
/// </summary>
public sealed class StartExportHandler(
    IReportExportRepository exports,
    ReportWindows windows,
    ICurrentUser currentUser,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IReportExportSignal signal,
    RequestValidator validator)
{
    public async Task<ReportExportDto> HandleAsync(StartExportCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        var staffId = currentUser.RequireStaffId();
        WireEnum.TryParse<ReportKind>(command.Report, out var kind);
        var format = ReportFormat.Csv;

        if (command.Format is { } wireFormat)
        {
            WireEnum.TryParse(wireFormat, out format);
        }

        var definition = ReportCatalogue.Of(kind);

        // The screen's own scope, on top of the endpoint's report:export.
        currentUser.RequireScopes(definition.ReadScope);

        var branchId = command.BranchId is { Length: > 0 } given ? given.Trim() : null;
        var window = definition.Monthly
            ? windows.ResolveMonths(command.From, command.To, branchId, definition.DefaultSpan)
            : windows.Resolve(command.From, command.To, branchId, definition.DefaultSpan);

        var export = ReportExport.Request(kind, format, window.First, window.Last, window.BranchId, window.BranchIds, staffId);

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            exports.Add(export);

            audit.Record(new AuditEvent(
                AuditActions.ReportExported,
                AuditResources.ReportExport,
                export.Id,
                BranchId: export.BranchId,
                Details: new Dictionary<string, object?>
                {
                    ["report"] = WireEnum.Of(kind),
                    ["format"] = WireEnum.Of(format),
                    ["from"] = export.FromDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    ["to"] = export.ToDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                    ["branch_ids"] = export.AllBranches ? null : export.BranchIds.ToArray(),
                }));

            return Task.CompletedTask;
        }, cancellationToken);

        signal.ExportQueued();

        return ReportExportDto.Of(export);
    }
}
