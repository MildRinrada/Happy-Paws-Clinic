using FluentValidation;
using HappyPaws.Domain.Articles;

namespace HappyPaws.Application.Articles.Manage;

/// <summary>
/// The text rules shared by writing a draft and editing one. Lengths are measured after
/// <see cref="ArticleText"/> has cleaned the value, which is what is kept.
/// </summary>
internal static class ArticleFieldRules
{
    public static void Title<T>(IRuleBuilder<T, string?> rule) =>
        rule.Must(value => ArticleText.CleanLine(value ?? string.Empty).Length is > 0 and <= Article.TitleMaxLength)
            .WithMessage($"ต้องมีชื่อเรื่อง ยาวไม่เกิน {Article.TitleMaxLength} ตัวอักษร");

    public static void Summary<T>(IRuleBuilder<T, string?> rule) =>
        rule.Must(value => ArticleText.CleanLine(value ?? string.Empty).Length is > 0 and <= Article.SummaryMaxLength)
            .WithMessage($"ต้องมีคำโปรย ยาวไม่เกิน {Article.SummaryMaxLength} ตัวอักษร");

    public static void Body<T>(IRuleBuilder<T, string?> rule) =>
        rule.Must(value => ArticleText.CleanBody(value ?? string.Empty).Length is > 0 and <= Article.BodyMaxLength)
            .WithMessage($"ต้องมีเนื้อหา ยาวไม่เกิน {Article.BodyMaxLength:N0} ตัวอักษร");

    public static void Author<T>(IRuleBuilder<T, string?> rule) =>
        rule.Must(value => ArticleText.CleanLine(value ?? string.Empty).Length is > 0 and <= Article.AuthorMaxLength)
            .WithMessage($"ต้องมีชื่อผู้เขียน ยาวไม่เกิน {Article.AuthorMaxLength} ตัวอักษร");

    public static void ReadingMinutes<T>(IRuleBuilder<T, int?> rule) =>
        rule.InclusiveBetween(1, 120).WithMessage("เวลาอ่านต้องอยู่ระหว่าง 1 ถึง 120 นาที");

    public static void Slug<T>(IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(200).WithMessage(ArticleSlugs.InvalidMessage);
}
