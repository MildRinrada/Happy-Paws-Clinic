using HappyPaws.Domain.Common;
using HappyPaws.Domain.Staff;

namespace HappyPaws.UnitTests.Domain.Staff;

/// <summary>
/// The two kinds of value in <c>photo_path</c> (<c>docs/contract-extensions.md § 32</c>): a path on
/// the website, and <c>storage:</c> followed by a key made for that one member.
/// </summary>
public class StaffPhotoTests
{
    private const int ThisYear = 2026;
    private const string Hex = "0123456789abcdef0123456789abcdef";

    private static StaffMembership Member() => StaffMembership.Create("usr_1", "veterinarian", ["rol_1"], []);

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public void A_new_key_reads_back_as_an_uploaded_photo_of_that_member(string contentType, string extension)
    {
        var key = StaffPhoto.NewKey("stf_06GFF0ABCDEF", contentType);
        var photoPath = StaffPhoto.PhotoPathOf(key);

        Assert.StartsWith("storage:staff-photos/stf_06GFF0ABCDEF/photo-", photoPath, StringComparison.Ordinal);
        Assert.EndsWith(extension, key, StringComparison.Ordinal);
        Assert.True(StaffPhoto.IsUploaded(photoPath));
        Assert.Equal(key, StaffPhoto.StorageKeyOf(photoPath));
        Assert.Equal("stf_06GFF0ABCDEF", StaffPhoto.OwnerOf(photoPath));
        Assert.Null(StaffPhoto.SitePathOf(photoPath));
    }

    [Fact]
    public void Two_uploads_never_share_a_key() =>
        Assert.NotEqual(StaffPhoto.NewKey("stf_1", "image/png"), StaffPhoto.NewKey("stf_1", "image/png"));

    [Theory]
    [InlineData("/images/team/mint.jpg")]
    [InlineData("/storage:staff-photos/stf_1/photo-" + Hex + ".jpg")]
    public void A_site_path_is_not_an_uploaded_photo(string photoPath)
    {
        Assert.False(StaffPhoto.IsUploaded(photoPath));
        Assert.Null(StaffPhoto.StorageKeyOf(photoPath));
        Assert.Equal(photoPath, StaffPhoto.SitePathOf(photoPath));
    }

    [Fact]
    public void Nothing_is_neither()
    {
        Assert.False(StaffPhoto.IsUploaded(null));
        Assert.Null(StaffPhoto.StorageKeyOf(null));
        Assert.Null(StaffPhoto.SitePathOf(null));
    }

    [Theory]
    [InlineData("storage:staff-photos/stf_1/photo-" + Hex + ".svg")]
    [InlineData("storage:staff-photos/stf_1/photo-" + Hex + ".jpg.html")]
    [InlineData("storage:staff-photos/stf_1/photo-0123.jpg")]
    [InlineData("storage:staff-photos/stf_1/photo-0123456789ABCDEF0123456789ABCDEF.jpg")]
    [InlineData("storage:staff-photos/stf_1/../stf_2/photo-" + Hex + ".jpg")]
    [InlineData("storage:staff-photos/cus_1/photo-" + Hex + ".jpg")]
    [InlineData("storage:articles/art_1/cover-" + Hex + ".jpg")]
    [InlineData("storage:/staff-photos/stf_1/photo-" + Hex + ".jpg")]
    [InlineData("storage: staff-photos/stf_1/photo-" + Hex + ".jpg")]
    [InlineData("STORAGE:staff-photos/stf_1/photo-" + Hex + ".jpg")]
    [InlineData("storage:")]
    public void A_value_the_key_maker_could_not_have_made_has_no_key(string photoPath)
    {
        Assert.Null(StaffPhoto.StorageKeyOf(photoPath));
        Assert.Null(StaffPhoto.OwnerOf(photoPath));
    }

    [Fact]
    public void A_profile_takes_a_photo_uploaded_for_that_same_member()
    {
        var member = Member();
        var photoPath = StaffPhoto.PhotoPathOf(StaffPhoto.NewKey(member.Id, "image/webp"));

        member.ReplacePublicProfile(new StaffPublicProfile(PhotoPath: photoPath, ShowOnWebsite: true), ThisYear);

        Assert.Equal(photoPath, member.PhotoPath);
    }

    [Fact]
    public void A_profile_refuses_a_photo_uploaded_for_someone_else()
    {
        var member = Member();
        var someoneElses = StaffPhoto.PhotoPathOf(StaffPhoto.NewKey(Member().Id, "image/jpeg"));

        var refused = Assert.Throws<DomainException>(() =>
            member.ReplacePublicProfile(new StaffPublicProfile(PhotoPath: someoneElses), ThisYear));

        Assert.Equal("invalid_public_profile", refused.Code);
    }

    [Theory]
    [InlineData("storage:staff-photos/stf_1/photo-" + Hex + ".svg")]
    [InlineData("storage:https://example.com/me.jpg")]
    public void A_profile_refuses_a_malformed_upload_value(string photoPath) =>
        Assert.Throws<DomainException>(() =>
            Member().ReplacePublicProfile(new StaffPublicProfile(PhotoPath: photoPath), ThisYear));

    [Fact]
    public void A_site_path_still_works_as_it_did()
    {
        var member = Member();

        member.ReplacePublicProfile(new StaffPublicProfile(PhotoPath: "/images/team/mint.jpg"), ThisYear);

        Assert.Equal("/images/team/mint.jpg", member.PhotoPath);
    }
}
