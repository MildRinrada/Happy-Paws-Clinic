namespace HappyPaws.Application.Common.Abstractions;

/// <summary>
/// Applies pending schema migrations. Used only by the <c>migrate</c> operator command.
/// <para>
/// Migrations are applied explicitly at deploy and never at application start
/// (<c>docs/database.md § 7</c>) — a clinic instance that migrates on boot will eventually migrate
/// during an outage, mid-consultation. This is the explicit step, in a form that needs only the
/// published application: a container image has no SDK to run <c>dotnet ef</c> with.
/// </para>
/// </summary>
public interface IDatabaseMigrator
{
    /// <summary>Applies every pending migration and returns the names of the ones it applied.</summary>
    Task<IReadOnlyList<string>> ApplyPendingAsync(CancellationToken cancellationToken = default);
}
