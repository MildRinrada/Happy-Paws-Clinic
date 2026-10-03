using FluentValidation;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.Application.Clinics.ManageContacts;

/// <summary>
/// The checks a contact entry's fields share between adding and changing one, with Thai messages
/// on the field that failed. The entity checks the same rules again; these exist so a mistake comes
/// back as a <c>422</c> on its field rather than the entity's <c>409</c>.
/// </summary>
internal static class ContactRules
{
    public static bool IsKind(string? kind) => kind is not null && ContactKinds.All.Contains(kind.Trim().ToLowerInvariant());

    public static bool IsWebAddressOrBlank(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp));

    public static bool IsMapEmbedOrBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().StartsWith(ClinicContact.MapEmbedPrefix, StringComparison.Ordinal);

    public static void Kind<T>(IRuleBuilder<T, string?> rule) =>
        rule.Must(IsKind).WithMessage("ประเภทช่องทางติดต่อไม่ถูกต้อง");

    public static void Label<T>(IRuleBuilder<T, string?> rule) =>
        rule.NotEmpty().WithMessage("ต้องระบุชื่อที่แสดง")
            .MaximumLength(ClinicContact.LabelMax).WithMessage($"ชื่อที่แสดงยาวได้ไม่เกิน {ClinicContact.LabelMax} ตัวอักษร");

    public static void Value<T>(IRuleBuilder<T, string?> rule) =>
        rule.NotEmpty().WithMessage("ต้องระบุข้อมูลติดต่อ")
            .MaximumLength(ClinicContact.ValueMax).WithMessage($"ข้อมูลติดต่อยาวได้ไม่เกิน {ClinicContact.ValueMax} ตัวอักษร");

    public static void Url<T>(IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(ClinicContact.UrlMax).WithMessage($"ลิงก์ยาวได้ไม่เกิน {ClinicContact.UrlMax} ตัวอักษร")
            .Must(IsWebAddressOrBlank).WithMessage("ลิงก์ต้องขึ้นต้นด้วย http:// หรือ https://");

    public static void EmbedUrl<T>(IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(ClinicContact.UrlMax).WithMessage($"ลิงก์ยาวได้ไม่เกิน {ClinicContact.UrlMax} ตัวอักษร")
            .Must(IsMapEmbedOrBlank).WithMessage("ใช้ลิงก์จาก Google Maps เมนู แชร์ แล้ว ฝังแผนที่ (ขึ้นต้นด้วย https://www.google.com/maps/embed)");
}
