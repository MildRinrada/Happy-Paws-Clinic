using System.Net;
using System.Text;
using HappyPaws.Application.Identity;
using HappyPaws.Application.Staff;
using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Identity;
using HappyPaws.Infrastructure.Identity;
using HappyPaws.Infrastructure.Messaging;
using HappyPaws.Infrastructure.Messaging.Sms;
using HappyPaws.Infrastructure.Staff;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HappyPaws.IntegrationTests.Messaging;

/// <summary>
/// The SMS and email senders behind sign-in and staff invitations, without a gateway or a mail
/// server: what they hand to the transport, which channel a reset link takes, and that a refused
/// send is logged rather than thrown.
/// </summary>
public class MessagingTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly string ClinicName = ClinicProfile.CreateDefault().Name;

    private static readonly IOptions<SignInDeliveryOptions> SignIn =
        Options.Create(new SignInDeliveryOptions { PasswordResetUrlTemplate = "https://clinic.test/reset-password#token={token}" });

    private static DeliveringPasswordResetSender ResetSender(RecordingEmail? email, RecordingSms? sms) =>
        new(
            email,
            sms,
            new LoggingPasswordResetSender(SignIn, NullLogger<LoggingPasswordResetSender>.Instance),
            EmptySettingsStore.Reader(),
            SignIn,
            NullLogger<DeliveringPasswordResetSender>.Instance);

    private static readonly PasswordResetMessage Reset =
        new("usr_1", "owner@clinic.test", "0812345678", "tok/en+1", DateTimeOffset.UtcNow.AddMinutes(30));

    [Fact]
    public async Task ThaiBulkSms_gets_the_number_the_text_and_the_key_pair_as_basic_auth()
    {
        var handler = new RecordingHttpHandler();
        var transport = new ThaiBulkSmsTransport(
            new SingleClientFactory(handler),
            Options.Create(new SmsOptions { Provider = "thaibulksms", ApiKey = "key", ApiSecret = "secret", Sender = "HappyPaws", BaseUrl = "https://sms.test/" }));

        await transport.SendAsync("0812345678", "รหัส 123456", Token);

        var (request, body) = Assert.Single(handler.Sent);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://sms.test/sms", request.RequestUri!.ToString());
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal("key:secret", Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!)));

        var form = body.Split('&').Select(pair => pair.Split('=')).ToDictionary(pair => pair[0], pair => Uri.UnescapeDataString(pair[1].Replace('+', ' ')));
        Assert.Equal("0812345678", form["msisdn"]);
        Assert.Equal("รหัส 123456", form["message"]);
        Assert.Equal("HappyPaws", form["sender"]);
    }

    [Fact]
    public async Task A_gateway_refusal_is_an_exception_naming_the_status()
    {
        var handler = new RecordingHttpHandler(HttpStatusCode.BadRequest, """{"error":{"name":"INSUFFICIENT_CREDIT"}}""");
        var transport = new ThaiBulkSmsTransport(
            new SingleClientFactory(handler),
            Options.Create(new SmsOptions { Provider = "thaibulksms", ApiKey = "key", ApiSecret = "secret" }));

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => transport.SendAsync("0812345678", "x", Token));

        Assert.Contains("INSUFFICIENT_CREDIT", error.Message);
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
    }

    [Fact]
    public async Task A_code_goes_by_SMS_in_one_Thai_part()
    {
        var sms = new RecordingSms();
        var sender = new SmsOtpSender(sms, EmptySettingsStore.Reader(), NullLogger<SmsOtpSender>.Instance);

        await sender.SendAsync("0812345678", "482913", OtpPurpose.Login, DateTimeOffset.UtcNow.AddMinutes(5), Token);

        var (phone, text) = Assert.Single(sms.Sent);
        Assert.Equal("0812345678", phone);
        Assert.Contains("482913", text);
        Assert.Contains(ClinicName, text);
        Assert.True(text.Length <= 70, $"{text.Length} characters is more than one Thai SMS part");
    }

    [Fact]
    public async Task A_reset_link_goes_by_email_when_the_account_has_one()
    {
        var email = new RecordingEmail();
        var sms = new RecordingSms();

        await ResetSender(email, sms).SendAsync(Reset, Token);

        var sent = Assert.Single(email.Sent);
        Assert.Equal("owner@clinic.test", sent.To);
        Assert.Contains("https://clinic.test/reset-password#token=tok%2Fen%2B1", sent.TextBody);
        Assert.Contains("https://clinic.test/reset-password#token=tok%2Fen%2B1", sent.HtmlBody);
        Assert.Empty(sms.Sent);
    }

    [Fact]
    public async Task A_reset_link_goes_by_SMS_when_there_is_no_address_or_no_email_set_up()
    {
        var sms = new RecordingSms();

        await ResetSender(new RecordingEmail(), sms).SendAsync(Reset with { Email = null }, Token);
        await ResetSender(null, sms).SendAsync(Reset, Token);

        Assert.Equal(2, sms.Sent.Count);
        Assert.All(sms.Sent, sent => Assert.Contains("reset-password#token=", sent.Text));
    }

    [Fact]
    public async Task A_refused_send_is_logged_not_thrown()
    {
        await ResetSender(new RecordingEmail(fail: true), null).SendAsync(Reset, Token);
        await new SmsOtpSender(new RecordingSms(fail: true), EmptySettingsStore.Reader(), NullLogger<SmsOtpSender>.Instance)
            .SendAsync("0812345678", "000000", OtpPurpose.Register, DateTimeOffset.UtcNow, Token);
    }

    [Fact]
    public async Task An_invitation_email_carries_the_link_and_escapes_the_name_in_HTML()
    {
        var email = new RecordingEmail();
        var sender = new EmailStaffInvitationSender(
            email,
            EmptySettingsStore.Reader(),
            Options.Create(new StaffInvitationOptions { AcceptUrlTemplate = "https://clinic.test/invitations/{id}#token={token}" }),
            NullLogger<EmailStaffInvitationSender>.Instance);

        await sender.SendAsync(new StaffInvitationMessage("inv_1", "abc", "vet@clinic.test", "<b>หมอ</b>", DateTimeOffset.UtcNow.AddDays(7)), Token);

        var sent = Assert.Single(email.Sent);
        Assert.Equal("vet@clinic.test", sent.To);
        Assert.Contains("https://clinic.test/invitations/inv_1#token=abc", sent.TextBody);
        Assert.Contains("&lt;b&gt;หมอ&lt;/b&gt;", sent.HtmlBody);
        Assert.DoesNotContain("<b>หมอ</b>", sent.HtmlBody);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void The_senders_follow_configuration_and_fall_back_to_the_log(bool configured, bool delivering)
    {
        var settings = new Dictionary<string, string?>();

        if (configured)
        {
            settings["Messaging:Email:Host"] = "smtp.clinic.test";
            settings["Messaging:Email:FromAddress"] = "no-reply@clinic.test";
            settings["Messaging:Sms:Provider"] = "thaibulksms";
            settings["Messaging:Sms:ApiKey"] = "key";
            settings["Messaging:Sms:ApiSecret"] = "secret";
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMessaging(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        using var provider = services.BuildServiceProvider();

        Assert.Equal(delivering, provider.GetRequiredService<IOtpSender>() is SmsOtpSender);
        Assert.Equal(delivering, provider.GetRequiredService<IStaffInvitationSender>() is EmailStaffInvitationSender);
        Assert.IsType<DeliveringPasswordResetSender>(provider.GetRequiredService<IPasswordResetSender>());
    }
}
