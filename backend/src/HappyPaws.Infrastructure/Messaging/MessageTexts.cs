using System.Globalization;
using System.Net;
using HappyPaws.Infrastructure.Messaging.Email;

namespace HappyPaws.Infrastructure.Messaging;

/// <summary>
/// What the sign-in and invitation messages say, in Thai. A Thai SMS part holds 70 characters, so
/// the code message is kept short: with a clinic name of up to about twenty characters it is one
/// part, one credit. It does not say whether the code is for signing up or signing in; the person
/// asked for it a moment ago and knows.
/// </summary>
internal static class MessageTexts
{
    private static readonly TimeZoneInfo ClinicTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    private static readonly CultureInfo Thai = CultureInfo.GetCultureInfo("th-TH");

    public static string OtpSms(string clinicName, string code) =>
        $"{clinicName} รหัส OTP {code} ใช้ได้ 5 นาที ห้ามบอกผู้อื่น";

    public static string ResetSms(string clinicName, string link, DateTimeOffset expiresAt) =>
        $"{clinicName}: ตั้งรหัสผ่านใหม่ที่ {link} ใช้ได้ถึง {Time(expiresAt)} น.";

    public static EmailMessage ResetEmail(string clinicName, string to, string link, DateTimeOffset expiresAt)
    {
        var subject = $"ตั้งรหัสผ่านใหม่ {clinicName}";
        var lines = new[]
        {
            "มีคำขอตั้งรหัสผ่านใหม่สำหรับบัญชีของคุณ",
            $"กดลิงก์นี้เพื่อตั้งรหัสผ่านใหม่ ลิงก์ใช้ได้ถึง {Time(expiresAt)} น. และใช้ได้ครั้งเดียว",
        };
        const string ignore = "ถ้าคุณไม่ได้ขอ ไม่ต้องทำอะไร รหัสผ่านเดิมยังใช้ได้ตามปกติ";

        return Compose(to, null, subject, clinicName, lines, link, "ตั้งรหัสผ่านใหม่", ignore);
    }

    public static EmailMessage InvitationEmail(string clinicName, string to, string name, string link, DateTimeOffset expiresAt)
    {
        var subject = $"คำเชิญเข้าร่วมทีม {clinicName}";
        var lines = new[]
        {
            $"สวัสดีคุณ{name}",
            $"{clinicName} เชิญคุณเข้าใช้ระบบจัดการคลินิก กดลิงก์นี้เพื่อตั้งรหัสผ่านและยืนยันตัวตนสองขั้นตอน",
            $"ลิงก์ใช้ได้ถึงวันที่ {Day(expiresAt)} และใช้ได้ครั้งเดียว",
        };
        const string ignore = "ถ้าคุณไม่รู้จักคลินิกนี้ ไม่ต้องทำอะไร ลิงก์จะหมดอายุเอง";

        return Compose(to, name, subject, clinicName, lines, link, "รับคำเชิญ", ignore);
    }

    private static EmailMessage Compose(
        string to,
        string? toName,
        string subject,
        string clinicName,
        string[] lines,
        string link,
        string button,
        string footer)
    {
        var text = string.Join("\n\n", [.. lines, link, footer, clinicName]);

        static string encode(string value) => WebUtility.HtmlEncode(value);
        var paragraphs = string.Concat(lines.Select(line => $"<p style=\"margin:0 0 14px\">{encode(line)}</p>"));
        var html =
            "<div style=\"font-family:sans-serif;font-size:15px;line-height:1.6;color:#1f2a2e;max-width:520px\">"
            + paragraphs
            + $"<p style=\"margin:20px 0\"><a href=\"{encode(link)}\" style=\"background:#217A71;color:#fff;padding:10px 18px;border-radius:8px;text-decoration:none\">{encode(button)}</a></p>"
            + $"<p style=\"margin:0 0 14px;font-size:13px;color:#5a6663;word-break:break-all\">{encode(link)}</p>"
            + $"<p style=\"margin:0 0 14px;font-size:13px;color:#5a6663\">{encode(footer)}</p>"
            + $"<p style=\"margin:0;font-size:13px;color:#5a6663\">{encode(clinicName)}</p>"
            + "</div>";

        return new EmailMessage(to, toName, subject, text, html);
    }

    private static string Time(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, ClinicTimeZone).ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string Day(DateTimeOffset instant) =>
        TimeZoneInfo.ConvertTime(instant, ClinicTimeZone).ToString("d MMMM yyyy", Thai);
}
