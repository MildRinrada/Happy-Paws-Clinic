using HappyPaws.Application.Access;
using HappyPaws.Application.Auditing;
using HappyPaws.Application.Clinics;
using HappyPaws.Application.Common.Abstractions;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Application.Identity;
using HappyPaws.Domain.Auditing;
using HappyPaws.Domain.Identity;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.SeedStaff;

/// <summary>What <c>seed-staff</c> did.</summary>
/// <param name="Created">Sign-in addresses created by this run.</param>
/// <param name="Skipped">Addresses that already had an account and were left exactly as they were.</param>
public sealed record SeedStaffResult(IReadOnlyList<string> Created, IReadOnlyList<string> Skipped);

/// <summary>
/// Adds the people in <see cref="SampleStaff"/> so the team page and the staff list have someone in
/// every department. Run by the operator's <c>seed-staff</c> command, never by a request.
/// <para>
/// <b>The accounts cannot sign in.</b> Each gets a random password that is thrown away and no
/// second factor, so a demo database never holds a login anybody knows. To use one, an operator
/// runs <c>reset-credentials</c> for it, the same as for a locked-out owner.
/// </para>
/// <para>
/// <b>Safe to run again.</b> An address that already has an account is skipped, not overwritten.
/// Needs <c>bootstrap</c> first: the people are given the starter roles and every open branch.
/// </para>
/// </summary>
public sealed class SeedStaffHandler(
    IIdentityRepository identity,
    IStaffRepository staff,
    IRoleRepository roles,
    IBranchRepository branches,
    OperatorCredentials credentials,
    IAuditTrail audit,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<SeedStaffResult> HandleAsync(CancellationToken cancellationToken = default)
    {
        var created = new List<string>();
        var skipped = new List<string>();

        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            // The transaction may be retried, so the result lists start again with it.
            created.Clear();
            skipped.Clear();

            var branchIds = (await branches.ListActiveAsync(token)).Select(branch => branch.Id).ToArray();

            if (branchIds.Length == 0)
            {
                throw NotBootstrapped("ยังไม่มีสาขา รัน bootstrap ก่อน");
            }

            var roleNames = SampleStaff.All
                .Select(member => member.Role)
                .OfType<string>()
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var roleIds = (await roles.FindByNamesAsync(roleNames, token))
                .ToDictionary(role => role.Name, role => role.Id, StringComparer.Ordinal);

            if (roleNames.FirstOrDefault(name => !roleIds.ContainsKey(name)) is { } missing)
            {
                throw NotBootstrapped($"ไม่มี role {missing} รัน bootstrap ก่อน");
            }

            var year = clock.NowInClinicTime().Year;

            foreach (var sample in SampleStaff.All)
            {
                var email = SampleStaff.EmailOf(sample);

                if (await identity.FindByEmailAsync(email, token) is not null)
                {
                    skipped.Add(email);
                    continue;
                }

                // The generated password is discarded unread: nobody ever knows it.
                var (hash, _) = credentials.NewPassword(null);
                var account = UserAccount.CreateStaff(sample.DisplayName, email, hash);

                var membership = StaffMembership.Create(
                    account.Id,
                    sample.StaffType,
                    sample.Role is { } role ? [roleIds[role]] : [],
                    branchIds,
                    sample.LicenseNo,
                    sample.Department);

                membership.ReplacePublicProfile(sample.Profile, year);

                identity.AddAccount(account);
                staff.Add(membership);
                created.Add(email);

                audit.Record(new AuditEvent(
                    AuditActions.StaffSeeded,
                    AuditResources.StaffMember,
                    membership.Id,
                    Details: new Dictionary<string, object?> { ["user_id"] = account.Id, ["email"] = email }));
            }

            await unitOfWork.SaveChangesAsync(token);
        }, cancellationToken);

        return new SeedStaffResult(created, skipped);
    }

    private static ValidationFailedException NotBootstrapped(string message) =>
        new(new Dictionary<string, string[]> { ["bootstrap"] = [message] });
}
