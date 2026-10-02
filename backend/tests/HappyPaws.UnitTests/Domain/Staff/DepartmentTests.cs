using HappyPaws.Application.Staff.SeedStaff;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Staff;

namespace HappyPaws.UnitTests.Domain.Staff;

public class DepartmentTests
{
    private const int ThisYear = 2026;

    private static StaffMembership Member(string staffType = "veterinarian", string? department = null) =>
        StaffMembership.Create("usr_1", staffType, ["rol_1"], [], department: department);

    [Theory]
    [InlineData("veterinarian", Departments.Veterinary)]
    [InlineData("vet_assistant", Departments.Nursing)]
    [InlineData("receptionist", Departments.FrontDesk)]
    [InlineData("cashier", Departments.Finance)]
    [InlineData("owner", Departments.Management)]
    [InlineData("clinic_admin", Departments.Management)]
    [InlineData("groomer", Departments.Support)]
    public void A_new_member_starts_in_the_department_their_staff_type_points_to(string staffType, string expected) =>
        Assert.Equal(expected, Member(staffType).Department);

    /// <summary>The migration's backfill repeats this mapping in SQL; a change here must change it too.</summary>
    [Fact]
    public void The_staff_type_defaults_are_the_ones_the_migration_backfilled()
    {
        var expected = new Dictionary<string, string>
        {
            ["veterinarian"] = "veterinary",
            ["vet_assistant"] = "nursing",
            ["receptionist"] = "front_desk",
            ["cashier"] = "finance",
            ["owner"] = "management",
            ["clinic_admin"] = "management",
        };

        Assert.Equal(expected.OrderBy(entry => entry.Key), Departments.StaffTypeDefaults.OrderBy(entry => entry.Key));
    }

    [Fact]
    public void A_department_given_outright_wins_over_the_staff_type() =>
        Assert.Equal(Departments.Management, Member("veterinarian", Departments.Management).Department);

    [Fact]
    public void An_unknown_department_is_refused()
    {
        var member = Member();

        var refused = Assert.Throws<DomainException>(() => member.MoveToDepartment("surgery"));

        Assert.Equal("invalid_department", refused.Code);
        Assert.Equal(Departments.Veterinary, member.Department);
    }

    [Fact]
    public void Nobody_is_on_the_website_until_their_profile_says_so() =>
        Assert.False(Member().ShowOnWebsite);

    [Fact]
    public void A_profile_is_replaced_whole_and_blank_text_is_cleared()
    {
        var member = Member();
        member.ReplacePublicProfile(new StaffPublicProfile("สัตวแพทย์ประจำ", "ผิวหนัง", "สวัสดีค่ะ", "/images/team/a.jpg", 2018, true), ThisYear);

        member.ReplacePublicProfile(new StaffPublicProfile(JobTitle: "  ", Specialty: "ทันตกรรม"), ThisYear);

        Assert.Null(member.JobTitle);
        Assert.Equal("ทันตกรรม", member.Specialty);
        Assert.Null(member.Bio);
        Assert.Null(member.PhotoPath);
        Assert.Null(member.CareerStartYear);
        Assert.False(member.ShowOnWebsite);
    }

    [Theory]
    [InlineData("https://example.com/me.jpg")]
    [InlineData("//example.com/me.jpg")]
    [InlineData("images/me.jpg")]
    public void A_photo_must_be_a_path_on_the_clinics_own_site(string photoPath)
    {
        var refused = Assert.Throws<DomainException>(() =>
            Member().ReplacePublicProfile(new StaffPublicProfile(PhotoPath: photoPath), ThisYear));

        Assert.Equal("invalid_public_profile", refused.Code);
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(ThisYear + 1)]
    public void A_start_year_outside_a_working_life_is_refused(int year) =>
        Assert.Throws<DomainException>(() =>
            Member().ReplacePublicProfile(new StaffPublicProfile(CareerStartYear: year), ThisYear));

    [Fact]
    public void Text_over_its_limit_is_refused() =>
        Assert.Throws<DomainException>(() => Member().ReplacePublicProfile(
            new StaffPublicProfile(Bio: new string('ก', StaffPublicProfile.BioMaxLength + 1)), ThisYear));

    [Fact]
    public void Years_of_experience_count_from_the_start_year()
    {
        var member = Member();
        member.ReplacePublicProfile(new StaffPublicProfile(CareerStartYear: 2018), ThisYear);

        Assert.Equal(8, member.YearsOfExperience(ThisYear));
        Assert.Null(Member().YearsOfExperience(ThisYear));
    }

    [Fact]
    public void Every_sample_person_is_valid_and_has_their_own_address()
    {
        Assert.All(SampleStaff.All, sample =>
        {
            var member = StaffMembership.Create("usr_1", sample.StaffType, [], [], sample.LicenseNo, sample.Department);
            member.ReplacePublicProfile(sample.Profile, ThisYear);

            Assert.Equal(sample.Department, member.Department);
        });

        Assert.Equal(SampleStaff.All.Count, SampleStaff.All.Select(SampleStaff.EmailOf).Distinct().Count());

        // Every department has someone, so the team page shows what it is for.
        Assert.All(Departments.All, department =>
            Assert.Contains(SampleStaff.All, sample => sample.Department == department.Code && sample.Profile.ShowOnWebsite));
    }
}
