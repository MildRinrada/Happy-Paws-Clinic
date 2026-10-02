using HappyPaws.Domain.Appointments;
using HappyPaws.Domain.Common;
using HappyPaws.Domain.Identity;

namespace HappyPaws.UnitTests.Domain.Appointments;

/// <summary>No-shows, the cancellation policy, appointment types, the waiting list and one-time codes.</summary>
public class AdvancedBookingRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 2);

    private static Appointment At(DateTimeOffset startsAt, string petId = "pet_1", string? staffId = "stf_1") =>
        Appointment.Book("brn_1", "cus_1", petId, "svc_1", staffId, startsAt, startsAt.AddMinutes(30));

    [Fact]
    public void A_no_show_waits_for_the_start_time()
    {
        var appointment = At(Now.AddMinutes(10));

        var exception = Assert.Throws<DomainException>(() => appointment.MarkNoShow(Now));
        Assert.Equal("no_show_too_early", exception.Code);

        appointment.MarkNoShow(Now.AddMinutes(15));
        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
    }

    [Fact]
    public void A_cancellation_inside_the_cutoff_is_late_and_outside_it_is_not()
    {
        var policy = CancellationPolicy.CreateDefault();

        Assert.True(policy.IsLate(Now, Now.AddHours(23)));
        Assert.False(policy.IsLate(Now, Now.AddHours(25)));
    }

    [Fact]
    public void The_fee_starts_once_the_allowance_is_used_up()
    {
        var policy = CancellationPolicy.CreateDefault();
        policy.Replace(cutoffHours: 24, feeSatang: 30_000, freeAllowance: 2, windowDays: 180);

        Assert.Equal(0, policy.FeeFor(2));
        Assert.Equal(30_000, policy.FeeFor(3));
    }

    [Fact]
    public void Appointment_types_always_keep_regular_and_refuse_duplicates()
    {
        var catalog = AppointmentTypeCatalog.CreateDefault();

        Assert.Throws<DomainException>(() => catalog.Replace([new AppointmentType("dental", "ทันตกรรม")]));
        Assert.Throws<DomainException>(() => catalog.Replace([new AppointmentType("regular", "ก"), new AppointmentType("regular", "ข")]));

        catalog.Replace([new AppointmentType("regular", "นัดปกติ"), new AppointmentType("dental", "ทันตกรรม", DurationMinutes: 60)]);

        Assert.Equal(60, catalog.Find("dental")!.DurationMinutes);
        Assert.Equal("regular", catalog.Find(null)!.Code);
        Assert.Null(catalog.Find("emergency"));
    }

    [Fact]
    public void A_waiting_entry_takes_a_freed_slot_only_for_its_service_dates_and_vet()
    {
        var entry = WaitingListEntry.Add("cus_2", "pet_2", "svc_1", Today, Today.AddDays(7), Today, "stf_9", staffId: "stf_1");

        Assert.True(entry.Fits(At(Now.AddDays(1)), Today.AddDays(1)));
        Assert.False(entry.Fits(At(Now.AddDays(10)), Today.AddDays(10)));
        Assert.False(entry.Fits(At(Now.AddDays(1), staffId: "stf_other"), Today.AddDays(1)));

        // Never the pet whose own visit was cancelled.
        Assert.False(entry.Fits(At(Now.AddDays(1), petId: "pet_2"), Today.AddDays(1)));
    }

    [Fact]
    public void A_code_is_spent_once_and_dies_after_five_wrong_guesses()
    {
        var verification = PhoneVerification.Issue("0812345678", OtpPurpose.Login, "RIGHT", Now);

        for (var i = 0; i < PhoneVerification.MaxAttempts; i++)
        {
            Assert.False(verification.TryConsume("WRONG", Now));
        }

        Assert.False(verification.TryConsume("RIGHT", Now));

        var fresh = PhoneVerification.Issue("0812345678", OtpPurpose.Login, "RIGHT", Now);
        Assert.True(fresh.TryConsume("RIGHT", Now));
        Assert.False(fresh.TryConsume("RIGHT", Now));
    }

    [Fact]
    public void A_code_expires_after_five_minutes()
    {
        var verification = PhoneVerification.Issue("0812345678", OtpPurpose.Register, "RIGHT", Now);

        Assert.False(verification.TryConsume("RIGHT", Now.AddMinutes(6)));
    }
}
