using FluentValidation;
using HappyPaws.Application.Clinics.Dtos;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Validation;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.ReplaceOpeningHours;

/// <summary><c>PUT /clinic/hours</c> — "ตั้งทั้งสัปดาห์ในคำขอเดียว".</summary>
public sealed record ReplaceOpeningHoursCommand(
    IReadOnlyList<DayHoursInput> Days,
    uint? IfMatchVersion = null);

/// <summary>
/// One weekday as the client sends it: <c>{ "day": "monday", "opens": "09:00", "closes": "18:00" }</c>,
/// or <c>{ "day": "sunday", "closed": true }</c> for a weekly closed day.
/// </summary>
public sealed record DayHoursInput(string Day, string? Opens = null, string? Closes = null, bool Closed = false);

public sealed class ReplaceOpeningHoursValidator : AbstractValidator<ReplaceOpeningHoursCommand>
{
    public ReplaceOpeningHoursValidator()
    {
        RuleFor(command => command.Days)
            .NotNull()
            .Must(days => days is { Count: 7 }
                && days.Select(day => day.Day?.Trim().ToLowerInvariant()).Distinct().Count() == 7)
            .WithMessage("ต้องระบุเวลาทำการให้ครบทั้ง 7 วัน วันละหนึ่งรายการ");

        RuleForEach(command => command.Days).ChildRules(day =>
        {
            day.RuleFor(input => input.Day)
                .Must(value => WireTime.TryParseDay(value, out _))
                .WithMessage("วันต้องเป็น monday ถึง sunday");

            day.RuleFor(input => input.Opens)
                .Must(value => WireTime.TryParseTime(value, out _))
                .WithMessage("เวลาต้องอยู่ในรูปแบบ HH:mm")
                .When(input => !input.Closed);

            day.RuleFor(input => input.Closes)
                .Must(value => WireTime.TryParseTime(value, out _))
                .WithMessage("เวลาต้องอยู่ในรูปแบบ HH:mm")
                .When(input => !input.Closed);

            day.RuleFor(input => input)
                .Must(input => WireTime.TryParseTime(input.Opens, out var opens)
                    && WireTime.TryParseTime(input.Closes, out var closes)
                    && closes > opens)
                .OverridePropertyName(nameof(DayHoursInput.Closes))
                .WithMessage("เวลาปิดต้องอยู่หลังเวลาเปิด")
                .When(input => !input.Closed
                    && WireTime.TryParseTime(input.Opens, out _)
                    && WireTime.TryParseTime(input.Closes, out _));
        });
    }
}

/// <summary>
/// Replaces the published week.
/// <para>
/// A whole-set replacement, as the method and the description both say — see
/// <c>docs/assumptions.md § 6</c>. A request naming six days is refused rather than merged, because
/// merging would make the seventh day's hours whatever they happened to be the last time anyone
/// looked.
/// </para>
/// </summary>
public sealed class ReplaceOpeningHoursHandler(
    IClinicSettingsStore settings,
    IHolidayRepository holidays,
    IUnitOfWork unitOfWork,
    RequestValidator validator,
    IClock clock)
{
    public async Task<OpeningHoursDto> HandleAsync(
        ReplaceOpeningHoursCommand command,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(command, cancellationToken);

        OpeningHours? updated = null;

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var hours = await settings.OpeningHoursForUpdateAsync(token);

            ConcurrencyGuard.Ensure(hours, command.IfMatchVersion);

            hours.ReplaceWeek(command.Days.Select(ToDay));

            updated = hours;
        }, cancellationToken);

        var today = DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);

        return OpeningHoursDto.From(updated!, await holidays.FromAsync(today, 100, cancellationToken));
    }

    /// <summary>The validator has already vouched for every value parsed here.</summary>
    private static DayHours ToDay(DayHoursInput input)
    {
        WireTime.TryParseDay(input.Day, out var day);

        if (input.Closed)
        {
            return DayHours.Closed(day);
        }

        WireTime.TryParseTime(input.Opens, out var opens);
        WireTime.TryParseTime(input.Closes, out var closes);

        return new DayHours(day, opens, closes);
    }
}
