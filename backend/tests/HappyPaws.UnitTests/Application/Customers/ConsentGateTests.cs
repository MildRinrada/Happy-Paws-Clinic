using System.Reflection;
using HappyPaws.Application.Customers;
using HappyPaws.Application.Notifications;
using HappyPaws.Domain.Customers;

namespace HappyPaws.UnitTests.Application.Customers;

/// <summary>
/// How a message is classified for consent, and the one mistake that would be invisible.
/// </summary>
public class ConsentGateTests
{
    /// <summary>Every constant on <see cref="NotificationTemplates"/>, read reflectively.</summary>
    public static TheoryData<string> Templates
    {
        get
        {
            var data = new TheoryData<string>();

            // `field` is a contextual keyword inside a property accessor from C# 14, hence
            // `constant`.
            var constants = typeof(NotificationTemplates)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(constant => constant.IsLiteral && constant.FieldType == typeof(string));

            foreach (var constant in constants)
            {
                data.Add((string)constant.GetRawConstantValue()!);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Templates))]
    public void Every_declared_template_is_classified_deliberately(string templateKey)
    {
        // An unlisted template falls through to Marketing, which is the safe direction for an
        // accident but the wrong answer for a queue call — and nothing at runtime would say so:
        // the push would simply stop arriving for every customer who never opted into marketing.
        // This test is what turns that silence into a build failure.
        Assert.NotEqual(ConsentPurpose.Marketing, ConsentGate.PurposeOf(templateKey));
    }

    [Fact]
    public void An_unknown_template_is_treated_as_marketing()
    {
        // Restrictive by default. A template nobody classified must not be able to reach a customer
        // who has opted out of everything optional.
        Assert.Equal(ConsentPurpose.Marketing, ConsentGate.PurposeOf("promotions.spring_campaign"));
    }

    [Fact]
    public void Only_care_related_messaging_is_on_by_default()
    {
        Assert.True(CustomerConsent.DefaultFor(ConsentPurpose.ServiceMessages));

        Assert.False(CustomerConsent.DefaultFor(ConsentPurpose.Marketing));
        Assert.False(CustomerConsent.DefaultFor(ConsentPurpose.MedicalRecordSharing));
        Assert.False(CustomerConsent.DefaultFor(ConsentPurpose.ExtendedDataProcessing));
    }
}
