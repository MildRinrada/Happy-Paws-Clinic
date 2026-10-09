using System.Security.Cryptography;
using System.Text;
using HappyPaws.Application.Integrations.GetCalendarFeed;
using HappyPaws.Application.Integrations.Webhooks;
using HappyPaws.Domain.Appointments;

namespace HappyPaws.UnitTests.Application.Integrations;

/// <summary>
/// The signature a receiver checks: HMAC-SHA256 of <c>timestamp.body</c> under the secret,
/// written <c>sha256=&lt;hex&gt;</c>. Checked against an independent computation.
/// </summary>
public class WebhookSignatureTests
{
    private const string Secret = "whsec_test_secret";
    private const string Body = """{"id":"evt_1","type":"invoice.issued","occurred_at":"2026-10-09T03:00:00Z","data":{}}""";

    [Fact]
    public void The_signature_is_hmac_sha256_of_timestamp_dot_body()
    {
        var expected = "sha256=" + Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes("1760000000." + Body)));

        Assert.Equal(expected, WebhookSignature.Sign(Secret, 1_760_000_000, Body));
    }

    [Fact]
    public void A_receiver_can_verify_and_spots_any_change()
    {
        var signature = WebhookSignature.Sign(Secret, 1_760_000_000, Body);

        Assert.True(WebhookSignature.Verify(Secret, 1_760_000_000, Body, signature));
        Assert.False(WebhookSignature.Verify(Secret, 1_760_000_001, Body, signature));
        Assert.False(WebhookSignature.Verify(Secret, 1_760_000_000, Body + " ", signature));
        Assert.False(WebhookSignature.Verify("whsec_other", 1_760_000_000, Body, signature));
    }

    [Fact]
    public void Secrets_are_long_and_never_repeat()
    {
        var first = WebhookSignature.NewSecret();

        Assert.StartsWith("whsec_", first, StringComparison.Ordinal);
        Assert.True(first.Length >= 40);
        Assert.NotEqual(first, WebhookSignature.NewSecret());
    }
}

/// <summary>The iCalendar writer: escaping, CRLF, and folding that never cuts a Thai character.</summary>
public class IcsCalendarTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 9, 3, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Text_is_escaped()
    {
        Assert.Equal("a\\, b\\; c\\\\ d\\ne", IcsCalendar.Escape("a, b; c\\ d\ne"));
    }

    [Fact]
    public void Long_thai_lines_are_folded_at_75_octets_on_character_boundaries()
    {
        var line = "SUMMARY:" + string.Concat(Enumerable.Repeat("ตรวจสุขภาพประจำปี ", 10));

        var folded = IcsCalendar.Fold(line);
        var parts = folded.Split("\r\n");

        Assert.True(parts.Length > 1);
        Assert.All(parts, part => Assert.True(Encoding.UTF8.GetByteCount(part) <= 75));
        Assert.All(parts.Skip(1), part => Assert.StartsWith(" ", part, StringComparison.Ordinal));
        Assert.Equal(line, string.Concat(parts.Select((part, index) => index == 0 ? part : part[1..])));
    }

    [Fact]
    public void A_feed_has_one_event_per_appointment_and_ends_lines_with_crlf()
    {
        var feed = IcsCalendar.Write(
            "คลินิก",
            "happypaws",
            [
                new CalendarEntry("apt_1", Now.AddHours(1), Now.AddHours(2), AppointmentStatus.Booked, "ตรวจสุขภาพ", "ลูน่า", "สาขาหนึ่ง", Now),
                new CalendarEntry("apt_2", Now.AddHours(3), Now.AddHours(4), AppointmentStatus.Cancelled, "ฉีดวัคซีน", "มิโล", "สาขาหนึ่ง", Now),
                new CalendarEntry("apt_3", Now.AddHours(3), Now.AddHours(4), AppointmentStatus.Draft, "ฉีดวัคซีน", "มิโล", "สาขาหนึ่ง", Now),
            ],
            Now);

        Assert.StartsWith("BEGIN:VCALENDAR\r\n", feed, StringComparison.Ordinal);
        Assert.EndsWith("END:VCALENDAR\r\n", feed, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", feed.Replace("\r\n", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("UID:apt_1@happypaws", feed, StringComparison.Ordinal);
        Assert.Contains("DTSTART:20261009T040000Z", feed, StringComparison.Ordinal);
        Assert.Contains("STATUS:CANCELLED", feed, StringComparison.Ordinal);
        Assert.DoesNotContain("apt_3", feed, StringComparison.Ordinal);
    }
}
