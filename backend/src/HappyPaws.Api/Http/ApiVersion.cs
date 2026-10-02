namespace HappyPaws.Api.Http;

/// <summary>
/// The version segment of the contract's base URL — <c>https://api.happypaws.co.th/v1</c>.
/// <para>
/// Applied with <c>UsePathBase</c>, which strips the prefix when it is present and lets a request
/// through unchanged when it is not. So <c>/v1/clinic</c> is the address the contract gives and a
/// client should use, and <c>/clinic</c> keeps working for a reverse proxy that has already taken
/// the prefix off.
/// </para>
/// </summary>
public static class ApiVersion
{
    public const string PathBase = "/v1";
}
