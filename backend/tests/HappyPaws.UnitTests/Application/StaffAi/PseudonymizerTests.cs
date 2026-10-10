using HappyPaws.Application.StaffAi.Privacy;

namespace HappyPaws.UnitTests.Application.StaffAi;

/// <summary>Personal data out before the model, back in after it.</summary>
public class PseudonymizerTests
{
    [Fact]
    public void A_registered_name_phone_and_email_are_replaced_and_restored()
    {
        var p = new Pseudonymizer();
        var owner = p.Person("สมชาย ใจดี", PersonalKind.Owner);
        p.Register("0812345678", PersonalKind.Phone);
        p.Register("somchai@example.com", PersonalKind.Email);

        var masked = p.Mask("เจ้าของ สมชาย ใจดี โทร 0812345678 อีเมล somchai@example.com");

        Assert.Equal("[OWNER_1]", owner);
        Assert.DoesNotContain("สมชาย", masked);
        Assert.DoesNotContain("0812345678", masked);
        Assert.DoesNotContain("somchai@example.com", masked);
        Assert.Contains("[OWNER_1]", masked);
        Assert.Contains("[PHONE_1]", masked);
        Assert.Contains("[EMAIL_1]", masked);
        Assert.False(p.Leaks(masked));

        Assert.Equal("เรียนคุณสมชาย ใจดี", p.Restore("เรียนคุณ[OWNER_1]"));
    }

    [Theory]
    [InlineData("โทรกลับที่ 081-234-5678 ได้เลย", "081-234-5678")]
    [InlineData("เบอร์ 02 123 4567 ค่ะ", "02 123 4567")]
    [InlineData("call +66 81 234 5678 please", "+66 81 234 5678")]
    [InlineData("mail me at a.b-c@clinic.co.th", "a.b-c@clinic.co.th")]
    [InlineData("บัตรประชาชน 1-2345-67890-12-3", "1-2345-67890-12-3")]
    public void Unregistered_contact_details_in_free_text_are_masked_by_pattern(string text, string detail)
    {
        var p = new Pseudonymizer();

        var masked = p.Mask(text);

        Assert.DoesNotContain(detail, masked);
        Assert.Matches(@"\[(PHONE|EMAIL|ID)_1\]", masked);
        Assert.Equal(text, p.Restore(masked));
    }

    [Theory]
    [InlineData("น้ำหนัก 5.2 kg อุณหภูมิ 39.5")]
    [InlineData("Amoxicillin 250 mg วันละ 2 ครั้ง 7 วัน")]
    [InlineData("นัด 2026-10-12 เวลา 10:30")]
    public void Clinical_numbers_are_not_mistaken_for_contact_details(string text)
    {
        Assert.Equal(text, new Pseudonymizer().Mask(text));
    }

    [Fact]
    public void A_name_part_is_masked_after_a_title_or_on_its_own_but_not_inside_another_word()
    {
        var p = new Pseudonymizer();
        p.Person("คุณมานะ ทดสอบ", PersonalKind.Owner);

        Assert.Equal("ฝากคุณ[OWNER_1]ด้วย", p.Mask("ฝากคุณมานะด้วย"));
        Assert.Equal("ส่งผลให้ [OWNER_1] แล้ว", p.Mask("ส่งผลให้ มานะ แล้ว"));
        Assert.Equal("ผลทดสอบปกติ", p.Mask("ผลทดสอบปกติ"));
    }

    [Fact]
    public void Staff_and_owners_get_their_own_numbering_and_the_same_value_keeps_its_token()
    {
        var p = new Pseudonymizer();

        Assert.Equal("[OWNER_1]", p.Person("คุณเอ", PersonalKind.Owner));
        Assert.Equal("[STAFF_1]", p.Person("หมอบี สัตวแพทย์", PersonalKind.Staff));
        Assert.Equal("[STAFF_2]", p.Person("ซี ผู้ช่วย", PersonalKind.Staff));
        Assert.Equal("[STAFF_1]", p.Person("หมอบี สัตวแพทย์", PersonalKind.Staff));
        Assert.Null(p.Person("  ", PersonalKind.Staff));
        Assert.Equal(3, p.Tokens.Count);
    }

    [Fact]
    public void A_longer_registered_value_is_replaced_before_a_shorter_one_inside_it()
    {
        var p = new Pseudonymizer();
        p.Person("สมศรี", PersonalKind.Owner);
        p.Person("สมศรี มีสุข", PersonalKind.Owner);

        Assert.Equal("[OWNER_2] มาด้วย", p.Mask("สมศรี มีสุข มาด้วย"));
    }

    [Fact]
    public void A_title_on_its_own_is_not_taken_for_part_of_a_name()
    {
        var p = new Pseudonymizer();
        p.Person("สพ.ญ. มินท์ ศรีสุข", PersonalKind.Staff);

        Assert.Equal("ส่งต่อ สพ.ญ. ท่านอื่น", p.Mask("ส่งต่อ สพ.ญ. ท่านอื่น"));
        Assert.Equal("ฝากหมอ[STAFF_1]ดูต่อ", p.Mask("ฝากหมอมินท์ดูต่อ"));
    }

    [Fact]
    public void A_placeholder_the_model_made_up_is_left_visible_not_guessed()
    {
        var p = new Pseudonymizer();
        p.Person("คุณเอ", PersonalKind.Owner);

        Assert.Equal("คุณเอ และ [OWNER_7]", p.Restore("[OWNER_1] และ [OWNER_7]"));
    }
}
