namespace HappyPaws.Application.Common.Errors;

/// <summary>
/// The status codes the contract uses, declared here so the Application layer can state the
/// outcome of a use case without referencing ASP.NET Core.
/// </summary>
public static class HttpStatus
{
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int Locked = 423;
    public const int UnprocessableEntity = 422;
    public const int TooManyRequests = 429;
}
