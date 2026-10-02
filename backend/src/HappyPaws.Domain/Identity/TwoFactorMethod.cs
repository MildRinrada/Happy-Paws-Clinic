namespace HappyPaws.Domain.Identity;

/// <summary>The second factor methods the contract names: TOTP or SMS.</summary>
public enum TwoFactorMethod
{
    None,
    Totp,
    Sms,
}
