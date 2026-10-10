using HappyPaws.Application.Automation;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Automation;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Customers;

namespace HappyPaws.UnitTests.Domain.Reviews;

/// <summary>
/// The <c>visit_completed</c> trigger: whole days after the visit, and only under marketing consent
/// (<c>docs/contract-extensions.md § 36</c>).
/// </summary>
public class VisitCompletedRuleTests
{
    private static AutomationRule Rule(int? days, int? hours = null, string? service = null) =>
        AutomationRule.Create(
            "ชวนรีวิว", AutomationTrigger.VisitCompleted, days, hours, null, AutomationChannel.Auto,
            NotificationTemplates.ReviewInvitation, enabled: false, "stf_1", serviceId: service);

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-31)]
    public void An_invitation_goes_one_to_thirty_days_after_the_visit(int days)
    {
        Assert.Throws<DomainException>(() => Rule(days));
        Assert.Equal((-30, -1), AutomationRule.OffsetDaysRange(AutomationTrigger.VisitCompleted));
    }

    [Fact]
    public void The_approved_default_of_two_days_after_is_a_visit_rule()
    {
        var rule = Rule(-2);

        Assert.Equal(AutomationSubjectKind.Visit, rule.SubjectKind);
        Assert.Equal(new TimeOnly(9, 0), rule.SendTime);
    }

    [Fact]
    public void It_counts_whole_days_and_names_no_service()
    {
        Assert.Throws<DomainException>(() => Rule(days: null, hours: 48));
        Assert.Throws<DomainException>(() => Rule(-2, service: "svc_1"));
        Assert.False(AutomationRule.AllowsHours(AutomationTrigger.VisitCompleted));
    }

    [Fact]
    public void Asking_for_a_review_is_marketing()
    {
        Assert.Equal(NotificationTemplates.ReviewInvitation, AutomationMessages.TemplateFor(AutomationTrigger.VisitCompleted));
        Assert.Equal(ConsentPurpose.Marketing, ConsentGate.PurposeOf(NotificationTemplates.ReviewInvitation));
    }

    [Fact]
    public void The_inbox_copy_never_carries_the_link()
    {
        var request = new NotificationRequest(
            "cus_1",
            NotificationTemplates.ReviewInvitation,
            new Dictionary<string, string>
            {
                ["pet_name"] = "ลูน่า",
                ["date"] = "8 ต.ค. 2569",
                ["review_url"] = "https://clinic.example/review/hprv_secret",
            });

        var inbox = NotificationText.Render(request);

        Assert.Contains("ลูน่า", inbox.Title);
        Assert.DoesNotContain("hprv_", inbox.Title + inbox.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("{", inbox.Body, StringComparison.Ordinal);
    }
}
