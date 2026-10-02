using HappyPaws.Domain.Patients;

namespace HappyPaws.Application.Patients.Dtos;

/// <summary>A treatment plan and its sessions. A session with no <c>appointment_id</c> is planned but not booked.</summary>
public sealed record TreatmentPlanDto(
    string Id,
    string PetId,
    string Title,
    string ServiceId,
    string? ConsultationId,
    string? TemplateId,
    string? Notes,
    string? SeriesId,
    string CreatedByStaffId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TreatmentPlanSessionDto> Sessions)
{
    public static TreatmentPlanDto From(TreatmentPlan plan) =>
        new(
            plan.Id,
            plan.PetId,
            plan.Title,
            plan.ServiceId,
            plan.ConsultationId,
            plan.TemplateId,
            plan.Notes,
            plan.SeriesId,
            plan.CreatedByStaffId,
            plan.CreatedAt,
            [.. plan.Sessions.Select(session => new TreatmentPlanSessionDto(session.Number, session.PlannedAt, session.AppointmentId))]);
}

public sealed record TreatmentPlanSessionDto(int Number, DateTimeOffset PlannedAt, string? AppointmentId);
