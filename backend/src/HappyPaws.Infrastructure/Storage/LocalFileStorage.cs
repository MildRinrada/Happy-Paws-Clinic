using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace HappyPaws.Infrastructure.Storage;

/// <summary>
/// Object storage on the local disk, with S3-style signed URLs served by the API itself.
/// <para>
/// A URL carries everything needed to honour it — what it allows, at which key, for which content
/// type, up to how many bytes, until when — signed with HMAC-SHA256. Nothing about an upload is
/// kept server-side until the bytes arrive, so a URL that is never used leaves nothing behind.
/// </para>
/// <para>
/// Keys are made by use cases, never by clients, and are still checked here: a key that would
/// resolve outside the storage folder is refused, whatever signed it.
/// </para>
/// </summary>
internal sealed partial class LocalFileStorage : IFileStorage
{
    private const string Upload = "put";
    private const string Download = "get";
    private const string ContentTypeSuffix = ".content-type";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly byte[] _signingKey;
    private readonly string _root;
    private readonly string _baseUrl;
    private readonly IClock _clock;

    public LocalFileStorage(IOptions<StorageOptions> options, IOptions<JwtOptions> jwt, IClock clock)
    {
        var settings = options.Value;

        _signingKey = settings.SigningKey is { Length: >= 32 } key
            ? Encoding.UTF8.GetBytes(key)
            : HMACSHA256.HashData(Encoding.UTF8.GetBytes(jwt.Value.SigningKey), "happypaws-storage"u8.ToArray());

        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(settings.LocalRoot)
            ? Path.Combine(Path.GetTempPath(), "happypaws-storage")
            : settings.LocalRoot);

        _baseUrl = settings.PublicBaseUrl.TrimEnd('/');
        _clock = clock;
    }

    public UploadTicket CreateUpload(string key, string contentType, long maxBytes, TimeSpan lifetime)
    {
        var expiresAt = _clock.UtcNow + lifetime;
        var token = Sign(new Grant(Upload, SafeKey(key), contentType, maxBytes, expiresAt.ToUnixTimeSeconds()));

        return new UploadTicket(
            $"{_baseUrl}/storage/uploads/{token}",
            "PUT",
            new Dictionary<string, string> { ["Content-Type"] = contentType },
            expiresAt);
    }

    public string CreateDownloadUrl(string key, TimeSpan lifetime)
    {
        var token = Sign(new Grant(Download, SafeKey(key), null, 0, (_clock.UtcNow + lifetime).ToUnixTimeSeconds()));

        return $"{_baseUrl}/storage/files/{token}";
    }

    public async Task<ReceivedFile?> ReceiveAsync(
        string token,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (Verify(token, Upload) is not { } grant
            || !string.Equals(grant.ContentType, MediaTypeOf(contentType), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var path = PathOf(grant.Key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Written beside the target and moved into place, so a half-sent file is never readable
        // under the real key, and a body over the limit leaves nothing behind.
        var partial = $"{path}.{Guid.NewGuid():N}.partial";
        long written = 0;

        try
        {
            await using (var file = File.Create(partial))
            {
                var buffer = new byte[81_920];
                int read;

                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    written += read;
                    if (written > grant.MaxBytes)
                    {
                        return null;
                    }

                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            if (written == 0)
            {
                return null;
            }

            File.Move(partial, path, overwrite: true);
            await File.WriteAllTextAsync(path + ContentTypeSuffix, grant.ContentType, cancellationToken);

            return new ReceivedFile(grant.Key, written);
        }
        finally
        {
            if (File.Exists(partial))
            {
                File.Delete(partial);
            }
        }
    }

    public async Task<StoredFile?> OpenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (Verify(token, Download) is not { } grant)
        {
            return null;
        }

        var path = PathOf(grant.Key);

        if (!File.Exists(path))
        {
            return null;
        }

        var contentType = File.Exists(path + ContentTypeSuffix)
            ? await File.ReadAllTextAsync(path + ContentTypeSuffix, cancellationToken)
            : "application/octet-stream";

        return new StoredFile(File.OpenRead(path), contentType);
    }

    private string Sign(Grant grant)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(grant, Json);

        return $"{Base64Url(payload)}.{Base64Url(HMACSHA256.HashData(_signingKey, payload))}";
    }

    private Grant? Verify(string token, string operation)
    {
        var parts = token.Split('.');
        if (parts.Length != 2 || FromBase64Url(parts[0]) is not { } payload || FromBase64Url(parts[1]) is not { } signature)
        {
            return null;
        }

        // Constant-time, so the comparison does not leak how much of a forged signature was right.
        if (!CryptographicOperations.FixedTimeEquals(signature, HMACSHA256.HashData(_signingKey, payload)))
        {
            return null;
        }

        var grant = JsonSerializer.Deserialize<Grant>(payload, Json);

        return grant is not null
            && grant.Op == operation
            && grant.Exp > _clock.UtcNow.ToUnixTimeSeconds()
            && KeyPattern().IsMatch(grant.Key)
                ? grant
                : null;
    }

    private string PathOf(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));

        return path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? path
            : throw new InvalidOperationException($"Storage key resolves outside the storage root: {key}");
    }

    private static string SafeKey(string key) =>
        KeyPattern().IsMatch(key) && !key.Contains("..", StringComparison.Ordinal)
            ? key
            : throw new ArgumentException($"Unsafe storage key: {key}", nameof(key));

    /// <summary><c>image/jpeg; charset=…</c> → <c>image/jpeg</c>.</summary>
    private static string? MediaTypeOf(string? contentType) => contentType?.Split(';')[0].Trim();

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[]? FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - (padded.Length % 4)) % 4);

        try
        {
            return Convert.FromBase64String(padded);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_][A-Za-z0-9_./-]{0,250}$")]
    private static partial Regex KeyPattern();

    /// <summary>What a URL allows. Short names: this is inside every URL.</summary>
    private sealed record Grant(string Op, string Key, string? ContentType, long MaxBytes, long Exp);
}
