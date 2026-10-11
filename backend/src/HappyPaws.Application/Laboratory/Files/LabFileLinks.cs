namespace HappyPaws.Application.Laboratory.Files;

/// <summary>How long the signed URLs for a lab request's files last (<c>docs/contract-extensions.md § 54</c>).</summary>
public static class LabFileLinks
{
    /// <summary>Time to pick a file and send it on a slow connection, as for every other upload.</summary>
    public static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// A link to open one file is asked for at the moment it is opened, so it only has to outlive
    /// the redirect and a slow download. Short, because whoever holds it can open the file.
    /// </summary>
    public static readonly TimeSpan OpenLifetime = TimeSpan.FromMinutes(5);
}

/// <summary>A link to open one file, and when it stops working.</summary>
public sealed record LabFileLinkDto(string Url, DateTimeOffset ExpiresAt);
