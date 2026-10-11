using HappyPaws.Application.Clinics.BrandImages;
using HappyPaws.Application.Clinics.UpdateClinicProfile;
using HappyPaws.Domain.Clinics;

namespace HappyPaws.UnitTests.Application.Clinics;

/// <summary>
/// What the logo and tab icon upload requests and the profile edit accept before anything is
/// stored (<c>docs/contract-extensions.md § 50</c>).
/// </summary>
public class BrandImageValidationTests
{
    private const string Hex = "0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData("image/png", 48_211, true)]
    [InlineData("image/jpeg", 5 * 1024 * 1024, true)]
    [InlineData("image/webp", 1, true)]
    [InlineData("image/svg+xml", 100, false)]
    [InlineData("image/gif", 100, false)]
    [InlineData("image/x-icon", 100, false)]
    [InlineData(null, 100, false)]
    [InlineData("image/png", 0, false)]
    [InlineData("image/png", 5 * 1024 * 1024 + 1, false)]
    public void Only_a_png_jpeg_or_webp_of_at_most_5_mb_is_accepted(string? contentType, long sizeBytes, bool valid)
    {
        foreach (var kind in Enum.GetValues<ClinicBrandImageKind>())
        {
            var result = new RequestBrandImageValidator().Validate(new RequestBrandImageCommand(kind, contentType, sizeBytes));

            Assert.Equal(valid, result.IsValid);
        }
    }

    [Theory]
    [InlineData("https://cdn.happypaws.test/logo.png", true)]
    [InlineData("storage:clinic-brand/logo-" + Hex + ".png", true)]
    [InlineData("", true)]
    [InlineData("storage:clinic-brand/tab-icon-" + Hex + ".png", false)]
    [InlineData("storage:clinic-brand/logo-" + Hex + ".svg", false)]
    [InlineData("storage:staff-photos/stf_1/photo-" + Hex + ".jpg", false)]
    [InlineData("storage:clinic-brand/../secrets.png", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("/logo.png", false)]
    public void The_logo_is_a_web_address_or_a_logo_upload_in_the_exact_shape_it_was_given(string logoUrl, bool valid)
    {
        var result = new UpdateClinicProfileValidator().Validate(new UpdateClinicProfileCommand(LogoUrl: logoUrl));

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData("storage:clinic-brand/tab-icon-" + Hex + ".png", true)]
    [InlineData("", true)]
    [InlineData("storage:clinic-brand/logo-" + Hex + ".png", false)]
    [InlineData("https://cdn.happypaws.test/icon.png", false)]
    [InlineData("storage:clinic-brand/tab-icon-" + Hex + ".svg", false)]
    public void The_tab_icon_is_a_tab_icon_upload_only(string tabIconUrl, bool valid)
    {
        var result = new UpdateClinicProfileValidator().Validate(new UpdateClinicProfileCommand(TabIconUrl: tabIconUrl));

        Assert.Equal(valid, result.IsValid);
    }
}
