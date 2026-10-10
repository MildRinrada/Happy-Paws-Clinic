using FluentValidation;
using HappyPaws.Application.Common;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients;

/// <summary>
/// The shape rules for sex and birth date, shared by registering a patient and by staff's edit, so
/// both answer <c>422</c> on the same field with the same words. Not in the API reference; see
/// <c>docs/contract-extensions.md § 28</c>.
/// </summary>
public static class PetProfileRules
{
    /// <summary>The clinic's today, the latest birth date taken.</summary>
    public static DateOnly Today(IClock clock) => DateOnly.FromDateTime(clock.NowInClinicTime().DateTime);

    /// <summary>Parses <c>male</c>, <c>female</c> or <c>unknown</c>; null for anything else or nothing.</summary>
    public static PetSex? SexOf(string? value) => WireEnum.TryParse<PetSex>(value, out var sex) ? sex : null;

    public static IRuleBuilderOptions<T, string?> MustBeASex<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(value => value is null || SexOf(value) is not null)
            .WithMessage($"เพศต้องเป็นหนึ่งใน: {WireEnum.Names<PetSex>()}");

    public static IRuleBuilderOptions<T, DateOnly?> MustBeABirthDate<T>(this IRuleBuilder<T, DateOnly?> rule, IClock clock) =>
        rule.Must(value => value is null || value <= Today(clock))
            .WithMessage("วันเกิดต้องไม่อยู่ในอนาคต")
            .Must(value => value is null || value >= Pet.EarliestBirthDate)
            .WithMessage("วันเกิดต้องไม่ก่อนปี 1900");
}
