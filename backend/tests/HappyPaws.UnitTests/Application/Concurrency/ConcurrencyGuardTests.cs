using HappyPaws.Application.Common.Concurrency;
using HappyPaws.Application.Common.Errors;
using HappyPaws.Domain.Common;

namespace HappyPaws.UnitTests.Application.Concurrency;

public class ConcurrencyGuardTests
{
    /// <summary>Stand-in aggregate; the guard only needs Id and Version.</summary>
    private sealed class FakeAppointment : Entity
    {
        public FakeAppointment(uint version)
        {
            Id = "apt_24810";
            Version = version;
        }
    }

    [Fact]
    public void Matching_version_passes()
    {
        ConcurrencyGuard.Ensure(new FakeAppointment(7), ifMatchVersion: 7);
    }

    [Fact]
    public void Stale_version_is_rejected_with_both_versions_reported()
    {
        // The client must be able to tell "someone else edited this" from "my request was wrong",
        // so the response carries what was sent and what is stored.
        var exception = Assert.Throws<VersionConflictException>(
            () => ConcurrencyGuard.Ensure(new FakeAppointment(9), ifMatchVersion: 7));

        Assert.Equal(ErrorCodes.VersionConflict, exception.Code);
        Assert.Equal(HttpStatus.Conflict, exception.StatusCode);
        Assert.Equal(7u, exception.ExpectedVersion);
        Assert.Equal(9u, exception.CurrentVersion);
    }

    [Fact]
    public void Absent_header_is_permitted_by_the_lenient_check()
    {
        ConcurrencyGuard.Ensure(new FakeAppointment(7), ifMatchVersion: null);
    }

    [Fact]
    public void Absent_header_is_rejected_by_the_strict_check()
    {
        // On endpoints where a blind overwrite would lose clinical or financial data, a missing
        // If-Match is itself the failure — otherwise the protection is only as good as the least
        // careful client.
        var exception = Assert.Throws<ValidationFailedException>(
            () => ConcurrencyGuard.EnsureRequired(new FakeAppointment(7), ifMatchVersion: null));

        Assert.Contains("If-Match", exception.Errors.Keys);
    }
}
