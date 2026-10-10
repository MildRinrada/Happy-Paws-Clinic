using HappyPaws.Domain.Common;

namespace HappyPaws.Domain.Staff;

/// <summary>
/// What the public team page may say about a member of staff. Everything else about them (email,
/// phone, roles, branches, licence) stays inside the clinic.
/// </summary>
/// <param name="JobTitle">Title as the public reads it, e.g. <c>สัตวแพทย์ประจำ</c>.</param>
/// <param name="Specialty">Field of work, e.g. <c>อายุรกรรมสัตว์เล็ก</c>.</param>
/// <param name="Bio">A few sentences in their own words.</param>
/// <param name="PhotoPath">A file under the website's <c>/public</c>, starting with <c>/</c>, or an uploaded photo (<see cref="StaffPhoto"/>).</param>
/// <param name="CareerStartYear">The year (AD) they started in this line of work; the page shows years of experience from it.</param>
/// <param name="ShowOnWebsite">Off until the person agrees to be listed.</param>
public sealed record StaffPublicProfile(
    string? JobTitle = null,
    string? Specialty = null,
    string? Bio = null,
    string? PhotoPath = null,
    int? CareerStartYear = null,
    bool ShowOnWebsite = false)
{
    public const int JobTitleMaxLength = 120;
    public const int SpecialtyMaxLength = 120;
    public const int BioMaxLength = 600;
    public const int PhotoPathMaxLength = 300;
    public const int EarliestCareerStartYear = 1950;
}

public sealed partial class StaffMembership
{
    public string? JobTitle { get; private set; }

    public string? Specialty { get; private set; }

    public string? Bio { get; private set; }

    public string? PhotoPath { get; private set; }

    public int? CareerStartYear { get; private set; }

    /// <summary>
    /// Whether the public team page lists them. Off by default: a name and face on the clinic's
    /// website is the person's to agree to, not something that follows from being hired.
    /// </summary>
    public bool ShowOnWebsite { get; private set; }

    public StaffPublicProfile PublicProfile =>
        new(JobTitle, Specialty, Bio, PhotoPath, CareerStartYear, ShowOnWebsite);

    /// <summary>Years in the profession as of <paramref name="currentYear"/>, or null when the start year is unknown.</summary>
    /// <param name="currentYear">The clinic's current year (AD).</param>
    public int? YearsOfExperience(int currentYear) =>
        CareerStartYear is { } start ? Math.Max(0, currentYear - start) : null;

    /// <summary>Replaces the whole public profile. Blank text clears a field.</summary>
    /// <param name="profile">The new profile.</param>
    /// <param name="currentYear">The clinic's current year (AD); a start year after it is refused.</param>
    public void ReplacePublicProfile(StaffPublicProfile profile, int currentYear)
    {
        var jobTitle = Text(profile.JobTitle, StaffPublicProfile.JobTitleMaxLength, "ตำแหน่งที่แสดง");
        var specialty = Text(profile.Specialty, StaffPublicProfile.SpecialtyMaxLength, "ความเชี่ยวชาญ");
        var bio = Text(profile.Bio, StaffPublicProfile.BioMaxLength, "แนะนำตัว");
        var photoPath = Text(profile.PhotoPath, StaffPublicProfile.PhotoPathMaxLength, "รูป");

        // An uploaded photo of this same member (§ 32), or a site-relative file. An outside URL
        // would let the page load anything from anywhere.
        if (StaffPhoto.IsUploaded(photoPath))
        {
            if (StaffPhoto.OwnerOf(photoPath) != Id)
            {
                throw Invalid("รูปที่อัปโหลดไม่ใช่รูปของพนักงานคนนี้ เลือกรูปใหม่จากฟอร์มโปรไฟล์");
            }
        }
        else if (photoPath is not null && (!photoPath.StartsWith('/') || photoPath.StartsWith("//", StringComparison.Ordinal)))
        {
            throw Invalid("รูปต้องเป็น path ในเว็บของคลินิก ขึ้นต้นด้วย / เช่น /images/team/mint.jpg");
        }

        if (profile.CareerStartYear is { } year
            && (year < StaffPublicProfile.EarliestCareerStartYear || year > currentYear))
        {
            throw Invalid($"ปีที่เริ่มทำงานต้องอยู่ระหว่าง {StaffPublicProfile.EarliestCareerStartYear} ถึง {currentYear}");
        }

        JobTitle = jobTitle;
        Specialty = specialty;
        Bio = bio;
        PhotoPath = photoPath;
        CareerStartYear = profile.CareerStartYear;
        ShowOnWebsite = profile.ShowOnWebsite;
    }

    private static string? Text(string? value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : throw Invalid($"{field}ยาวได้ไม่เกิน {maxLength} ตัวอักษร");
    }

    private static DomainException Invalid(string message) => new("invalid_public_profile", message);
}
