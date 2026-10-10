using HappyPaws.Application.PrivacyPolicies;
using HappyPaws.Domain.PrivacyPolicies;
using Microsoft.EntityFrameworkCore;

namespace HappyPaws.Infrastructure.Persistence.Repositories.PrivacyPolicies;

/// <summary>EF Core implementation of the privacy notice port (<c>docs/contract-extensions.md § 35</c>).</summary>
internal sealed class PrivacyPolicyRepository(HappyPawsDbContext db) : IPrivacyPolicyRepository
{
    public void Add(PrivacyPolicyVersion version) => db.Set<PrivacyPolicyVersion>().Add(version);

    public Task<PrivacyPolicyVersion?> FindAsync(string id, CancellationToken cancellationToken = default) =>
        db.Set<PrivacyPolicyVersion>().FirstOrDefaultAsync(version => version.Id == id, cancellationToken);

    public Task<PrivacyPolicyVersion?> CurrentAsync(CancellationToken cancellationToken = default) =>
        Newest().FirstOrDefaultAsync(cancellationToken);

    public Task<string?> CurrentLabelAsync(CancellationToken cancellationToken = default) =>
        Newest().Select(version => version.Label).FirstOrDefaultAsync(cancellationToken);

    public Task<PrivacyPolicyVersion?> FindPublishedAsync(string label, CancellationToken cancellationToken = default) =>
        db.Set<PrivacyPolicyVersion>().AsNoTracking()
            .FirstOrDefaultAsync(version => version.Label == label && version.PublishedAt != null, cancellationToken);

    public async Task<IReadOnlyList<PublishedPolicySummary>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
        await Newest()
            .Select(version => new PublishedPolicySummary(version.Label!, version.EffectiveDate!.Value, version.PublishedAt!.Value))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PrivacyPolicyVersion>> ListAsync(CancellationToken cancellationToken = default) =>
        await db.Set<PrivacyPolicyVersion>().AsNoTracking()
            .OrderByDescending(version => version.CreatedAt).ThenByDescending(version => version.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> LabelTakenAsync(string label, CancellationToken cancellationToken = default) =>
        db.Set<PrivacyPolicyVersion>().AnyAsync(version => version.Label == label, cancellationToken);

    /// <summary>Published versions, newest first. The id is time-ordered, so it breaks a tie.</summary>
    private IQueryable<PrivacyPolicyVersion> Newest() =>
        db.Set<PrivacyPolicyVersion>().AsNoTracking()
            .Where(version => version.PublishedAt != null)
            .OrderByDescending(version => version.PublishedAt).ThenByDescending(version => version.Id);
}
