using HappyPaws.Application.Integrations;
using Microsoft.AspNetCore.DataProtection;

namespace HappyPaws.Infrastructure.Webhooks;

/// <summary>
/// <see cref="ISecretProtector"/> over ASP.NET Core Data Protection: authenticated encryption
/// (AES-256-CBC with HMACSHA256 by default) under a key ring that rotates every 90 days and keeps
/// old keys for decryption. The key ring lives where <c>DataProtection:KeysDirectory</c> says, not
/// in the database; see <c>docs/development.md</c>.
/// </summary>
internal sealed class DataProtectionSecretProtector(IDataProtectionProvider provider) : ISecretProtector
{
    public string Protect(string plaintext, string purpose) => provider.CreateProtector(purpose).Protect(plaintext);

    public string Unprotect(string ciphertext, string purpose) => provider.CreateProtector(purpose).Unprotect(ciphertext);
}
