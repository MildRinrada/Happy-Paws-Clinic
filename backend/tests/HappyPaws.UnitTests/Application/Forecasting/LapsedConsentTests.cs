using HappyPaws.Application.Automation;
using HappyPaws.Application.Customers;
using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Customers;

namespace HappyPaws.UnitTests.Application.Forecasting;

/// <summary>
/// The "we miss you" reminder goes through the marketing purpose, never the care purpose
/// (<c>docs/contract-extensions.md § 33</c>): a customer who said nothing, or declined marketing
/// while keeping care messages on, is not sent it.
/// </summary>
public class LapsedConsentTests
{
    private static readonly string LapsedTemplate = AutomationMessages.TemplateFor(AutomationTrigger.CustomerLapsed);
    private static readonly string VaccineTemplate = AutomationMessages.TemplateFor(AutomationTrigger.VaccineDue);

    [Fact]
    public async Task A_customer_who_recorded_nothing_gets_vaccine_reminders_but_not_the_lapsed_message()
    {
        var gate = new ConsentGate(new Consents());

        Assert.True(await gate.MayReceiveAsync("cus_1", VaccineTemplate, TestContext.Current.CancellationToken));
        Assert.False(await gate.MayReceiveAsync("cus_1", LapsedTemplate, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Care_consent_alone_does_not_let_the_lapsed_message_through()
    {
        var gate = new ConsentGate(new Consents(Recorded(ConsentPurpose.ServiceMessages, true), Recorded(ConsentPurpose.Marketing, false)));

        Assert.False(await gate.MayReceiveAsync("cus_1", LapsedTemplate, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Marketing_consent_lets_it_through_even_with_care_messages_off()
    {
        var gate = new ConsentGate(new Consents(Recorded(ConsentPurpose.ServiceMessages, false), Recorded(ConsentPurpose.Marketing, true)));

        Assert.True(await gate.MayReceiveAsync("cus_1", LapsedTemplate, TestContext.Current.CancellationToken));
        Assert.False(await gate.MayReceiveAsync("cus_1", VaccineTemplate, TestContext.Current.CancellationToken));
    }

    private static CustomerConsent Recorded(ConsentPurpose purpose, bool granted) =>
        CustomerConsent.Record("cus_1", purpose, granted, ConsentSource.Portal, DateTimeOffset.UtcNow);

    private sealed class Consents(params CustomerConsent[] recorded) : IConsentRepository
    {
        public void Add(CustomerConsent consent) => throw new NotSupportedException();

        public Task<IReadOnlyList<CustomerConsent>> HistoryAsync(string customerId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CustomerConsent>>(recorded);

        public Task<IReadOnlyDictionary<ConsentPurpose, CustomerConsent?>> CurrentAsync(
            string customerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ConsentPurpose, CustomerConsent?>>(
                Enum.GetValues<ConsentPurpose>().ToDictionary(
                    purpose => purpose,
                    purpose => recorded.LastOrDefault(consent => consent.Purpose == purpose)));
    }
}
