using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations.Dtos;

/// <summary>
/// A template as <c>GET /staff/templates/treatments</c> returns it. <c>sessions</c> is, for a
/// course of visits, how many; null for a single visit.
/// </summary>
public sealed record TreatmentTemplateDto(
    string Id,
    string Name,
    string? Species,
    string? Description,
    IReadOnlyList<TemplateProcedureDto> Procedures,
    IReadOnlyList<TemplateMedicineDto> Medicines,
    string? Plan,
    int? Sessions,
    int? SessionIntervalDays)
{
    public static TreatmentTemplateDto From(TreatmentTemplate template) =>
        new(
            template.Id,
            template.Name,
            template.Species,
            template.Description,
            [.. template.Procedures.Select(item => new TemplateProcedureDto(item.Name, item.Quantity, item.ServiceId, item.Note))],
            [.. template.Medicines.Select(item => new TemplateMedicineDto(
                item.DrugName, item.Dose, item.Frequency, item.DurationDays, item.Quantity, item.Instructions))],
            template.Plan,
            template.Sessions,
            template.SessionIntervalDays);
}

public sealed record TemplateProcedureDto(string Name, int Quantity, string? ServiceId, string? Note);

public sealed record TemplateMedicineDto(
    string DrugName,
    string Dose,
    string Frequency,
    int DurationDays,
    int Quantity,
    string? Instructions);
