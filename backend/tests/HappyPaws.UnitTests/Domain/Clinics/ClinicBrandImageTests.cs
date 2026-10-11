using HappyPaws.Domain.Clinics;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Domain.Clinics;

/// <summary>
/// The clinic's uploaded logo and tab icon (<c>docs/contract-extensions.md § 50</c>): a
/// <c>storage:</c> value made for one kind of picture, beside the web address <c>logo_url</c>
/// always took, and the switch that hides the name beside a logo.
/// </summary>
public class ClinicBrandImageTests
{
    private const string Hex = "0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData(ClinicBrandImageKind.Logo, "image/jpeg", "storage:clinic-brand/logo-", ".jpg")]
    [InlineData(ClinicBrandImageKind.Logo, "image/png", "storage:clinic-brand/logo-", ".png")]
    [InlineData(ClinicBrandImageKind.TabIcon, "image/webp", "storage:clinic-brand/tab-icon-", ".webp")]
    public void A_new_key_reads_back_as_an_upload_of_its_own_kind(
        ClinicBrandImageKind kind, string contentType, string start, string extension)
    {
        var key = ClinicBrandImage.NewKey(kind, contentType);
        var value = ClinicBrandImage.ValueOf(key);

        Assert.StartsWith(start, value, StringComparison.Ordinal);
        Assert.EndsWith(extension, key, StringComparison.Ordinal);
        Assert.True(ClinicBrandImage.IsUploaded(value));
        Assert.Equal(key, ClinicBrandImage.StorageKeyOf(kind, value));
    }

    [Fact]
    public void A_logo_is_not_a_tab_icon_and_a_tab_icon_is_not_a_logo()
    {
        var logo = ClinicBrandImage.ValueOf(ClinicBrandImage.NewKey(ClinicBrandImageKind.Logo, "image/png"));
        var tabIcon = ClinicBrandImage.ValueOf(ClinicBrandImage.NewKey(ClinicBrandImageKind.TabIcon, "image/png"));

        Assert.Null(ClinicBrandImage.StorageKeyOf(ClinicBrandImageKind.TabIcon, logo));
        Assert.Null(ClinicBrandImage.StorageKeyOf(ClinicBrandImageKind.Logo, tabIcon));
    }

    [Fact]
    public void Two_uploads_never_share_a_key() =>
        Assert.NotEqual(
            ClinicBrandImage.NewKey(ClinicBrandImageKind.Logo, "image/png"),
            ClinicBrandImage.NewKey(ClinicBrandImageKind.Logo, "image/png"));

    [Fact]
    public void Svg_has_no_key() => Assert.False(ClinicBrandImage.Extensions.ContainsKey("image/svg+xml"));

    [Theory]
    [InlineData(null)]
    [InlineData("https://cdn.happypaws.test/logo.png")]
    public void A_web_address_or_nothing_is_not_an_upload(string? value)
    {
        Assert.False(ClinicBrandImage.IsUploaded(value));
        Assert.Null(ClinicBrandImage.StorageKeyOf(ClinicBrandImageKind.Logo, value));
    }

    [Theory]
    [InlineData("storage:clinic-brand/logo-" + Hex + ".svg")]
    [InlineData("storage:clinic-brand/logo-" + Hex + ".png.html")]
    [InlineData("storage:clinic-brand/logo-0123.png")]
    [InlineData("storage:clinic-brand/logo-0123456789ABCDEF0123456789ABCDEF.png")]
    [InlineData("storage:clinic-brand/../staff-photos/stf_1/photo-" + Hex + ".jpg")]
    [InlineData("storage:clinic-brand/brn_1/logo-" + Hex + ".png")]
    [InlineData("storage:clinic-brand/banner-" + Hex + ".png")]
    [InlineData("storage:staff-photos/stf_1/photo-" + Hex + ".jpg")]
    [InlineData("storage:articles/art_1/cover-" + Hex + ".jpg")]
    [InlineData("storage:/clinic-brand/logo-" + Hex + ".png")]
    [InlineData("storage: clinic-brand/logo-" + Hex + ".png")]
    [InlineData("STORAGE:clinic-brand/logo-" + Hex + ".png")]
    [InlineData("storage:")]
    public void A_value_the_key_maker_could_not_have_made_has_no_key(string value)
    {
        Assert.Null(ClinicBrandImage.StorageKeyOf(ClinicBrandImageKind.Logo, value));
        Assert.Null(ClinicBrandImage.StorageKeyOf(ClinicBrandImageKind.TabIcon, value));
    }

    [Fact]
    public void The_profile_takes_both_uploads_and_gives_them_back_unchanged()
    {
        var profile = ClinicProfile.CreateDefault();
        var logo = ClinicBrandImage.ValueOf(ClinicBrandImage.NewKey(ClinicBrandImageKind.Logo, "image/webp"));
        var tabIcon = ClinicBrandImage.ValueOf(ClinicBrandImage.NewKey(ClinicBrandImageKind.TabIcon, "image/png"));

        profile.Update(logoUrl: logo, tabIconUrl: tabIcon);

        Assert.Equal(logo, profile.LogoUrl);
        Assert.Equal(tabIcon, profile.TabIconUrl);
    }

    [Theory]
    [InlineData("storage:clinic-brand/logo-" + Hex + ".svg")]
    [InlineData("storage:clinic-brand/tab-icon-" + Hex + ".png")]
    [InlineData("storage:staff-photos/stf_1/photo-" + Hex + ".jpg")]
    [InlineData("storage:https://example.com/logo.png")]
    public void The_profile_refuses_a_malformed_or_foreign_logo(string value)
    {
        var refused = Assert.Throws<DomainException>(() => ClinicProfile.CreateDefault().Update(logoUrl: value));

        Assert.Equal("invalid_clinic_profile", refused.Code);
    }

    [Theory]
    [InlineData("https://cdn.happypaws.test/icon.png")]
    [InlineData("storage:clinic-brand/logo-" + Hex + ".png")]
    public void A_tab_icon_is_an_upload_and_nothing_else(string value) =>
        Assert.Throws<DomainException>(() => ClinicProfile.CreateDefault().Update(tabIconUrl: value));

    [Fact]
    public void A_web_address_still_works_and_an_empty_value_goes_back_to_the_paw()
    {
        var profile = ClinicProfile.CreateDefault();

        profile.Update(logoUrl: "https://cdn.happypaws.test/logo.png");
        Assert.Equal("https://cdn.happypaws.test/logo.png", profile.LogoUrl);

        profile.Update(
            logoUrl: ClinicBrandImage.ValueOf(ClinicBrandImage.NewKey(ClinicBrandImageKind.Logo, "image/png")),
            tabIconUrl: ClinicBrandImage.ValueOf(ClinicBrandImage.NewKey(ClinicBrandImageKind.TabIcon, "image/png")));
        profile.Update(logoUrl: string.Empty, tabIconUrl: string.Empty);

        Assert.Null(profile.LogoUrl);
        Assert.Null(profile.TabIconUrl);
    }

    [Fact]
    public void The_name_shows_beside_the_logo_until_switched_off_and_an_absent_switch_leaves_it()
    {
        var profile = ClinicProfile.CreateDefault();
        Assert.True(profile.ShowNameWithLogo);

        profile.Update(showNameWithLogo: false);
        profile.Update(licenseNo: "สพ. 1/2569");
        Assert.False(profile.ShowNameWithLogo);

        profile.Update(showNameWithLogo: true);
        Assert.True(profile.ShowNameWithLogo);
    }
}
