using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HappyPaws.Application.StaffAi.Privacy;
using HappyPaws.Application.StaffAi.Prompts;

namespace HappyPaws.Application.StaffAi.PetSummary;

/// <summary>The data block a summary is written from, the sources behind its references, and its fingerprint.</summary>
public sealed record PetSummaryInput(string Data, IReadOnlyList<SummarySource> Sources, string Fingerprint);

/// <summary>
/// Turns a patient's file into the lines the model reads, one record per line under a short
/// reference: <c>[R3] medicine 2026-09-01: ...</c>.
/// <para>
/// <b>What is sent.</b> The pet's name, species, breed, sex, neutering and age, and clinical facts.
/// Not the hospital number, microchip, owner, carers or staff. Free text written by people (the
/// complaint, an assessment, an allergy reaction, a medicine's instructions) goes through the
/// pseudonymizer, which masks any owner or carer named in it and anything shaped like a phone
/// number, email or national id.
/// </para>
/// <para>
/// <b>The fingerprint</b> is a SHA-256 of the prompt version and exactly this text. Same text,
/// same summary: the cached one is served and nothing is counted. A new visit, a dose, a result, a
/// change of age or a medicine running out all change the text and so the fingerprint.
/// </para>
/// </summary>
public static class PetSummaryAssembler
{
    public static PetSummaryInput Build(PetFile file, DateOnly today, Pseudonymizer pseudonymizer)
    {
        foreach (var person in file.People)
        {
            pseudonymizer.Person(person.Name, PersonalKind.Owner);
            pseudonymizer.Register(person.Phone, PersonalKind.Phone);
            pseudonymizer.Register(person.Email, PersonalKind.Email);
        }

        var lines = new List<string> { $"Today: {Day(today)}", Patient(file.Pet, today), "Records (newest first):" };
        var sources = new List<SummarySource>();

        void Add(string kind, string id, DateOnly date, string text)
        {
            var reference = $"R{sources.Count + 1}";
            sources.Add(new SummarySource(reference, kind, id, date));
            lines.Add($"[{reference}] {kind} {Day(date)}: {text}");
        }

        foreach (var allergy in file.Allergies)
        {
            Add("allergy", allergy.Id, allergy.RecordedOn, Join(
                allergy.Substance,
                $"severity {allergy.Severity}",
                allergy.Reaction is { Length: > 0 } reaction ? $"reaction: {pseudonymizer.Mask(reaction)}" : null));
        }

        foreach (var visit in file.Visits)
        {
            var diagnoses = visit.Diagnoses.Count == 0
                ? "no diagnosis recorded"
                : "diagnoses: " + string.Join("; ", visit.Diagnoses.Select(diagnosis =>
                    diagnosis.IsPrimary ? $"{pseudonymizer.Mask(diagnosis.Description)} (primary)" : pseudonymizer.Mask(diagnosis.Description)));

            Add("visit", visit.Id, visit.OpenedOn, Join(
                visit.ChiefComplaint is { Length: > 0 } complaint ? $"complaint: {Short(pseudonymizer.Mask(complaint), 200)}" : null,
                diagnoses,
                visit.Assessment is { Length: > 0 } assessment ? $"assessment: {Short(pseudonymizer.Mask(assessment), 240)}" : null));
        }

        foreach (var medicine in file.Medicines)
        {
            var lastDay = medicine.PrescribedOn.AddDays(Math.Max(medicine.DurationDays, 1) - 1);
            var status = lastDay >= today ? $"active until {Day(lastDay)}" : $"finished {Day(lastDay)}";

            Add("medicine", medicine.Id, medicine.PrescribedOn, Join(
                $"{medicine.DrugName} {medicine.Dose}",
                medicine.Frequency,
                $"{medicine.DurationDays} days",
                status,
                medicine.Instructions is { Length: > 0 } instructions ? $"instructions: {Short(pseudonymizer.Mask(instructions), 160)}" : null));
        }

        foreach (var weight in file.Weights)
        {
            Add("weight", weight.ConsultationId, weight.RecordedOn, $"{weight.WeightKg.ToString("0.##", CultureInfo.InvariantCulture)} kg");
        }

        foreach (var lab in file.Labs)
        {
            var tests = lab.Tests.Count == 0 ? "lab" : string.Join(", ", lab.Tests);
            var values = lab.AbnormalValues.Count == 0
                ? $"{lab.ResultCount} result(s), none flagged abnormal"
                : "abnormal: " + string.Join("; ", lab.AbnormalValues.Select(value => Join(
                    $"{value.Analyte} {value.Value}{(value.Unit is { Length: > 0 } unit ? " " + unit : string.Empty)}",
                    value.Reference is { Length: > 0 } range ? $"ref {range}" : null,
                    value.Flag.ToUpperInvariant())));

            Add("lab", lab.Id, lab.On, lab.ResultCount == 0 ? $"{tests}: no results yet" : $"{tests}: {values}");
        }

        foreach (var dose in file.Vaccinations)
        {
            Add("vaccination", dose.Id, dose.GivenOn, Join(
                dose.VaccineName,
                dose.NextDueOn is { } due ? $"next due {Day(due)}" : null));
        }

        if (sources.Count == 0)
        {
            lines.Add("(no records)");
        }

        var data = string.Join('\n', lines);

        return new PetSummaryInput(data, sources, Fingerprint(data));
    }

    /// <summary>SHA-256 of the prompt version and the data, as 64 lowercase hex characters.</summary>
    public static string Fingerprint(string data)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(PetSummaryPrompt.Version + "\n" + data));
        return Convert.ToHexStringLower(bytes);
    }

    private static string Patient(PetFacts pet, DateOnly today) =>
        "Patient: " + Join(
            pet.Name,
            pet.Species,
            pet.Breed,
            $"sex {pet.Sex}",
            pet.Neutered switch { true => "neutered", false => "not neutered", null => "neutering unknown" },
            pet.BirthDate is { } born ? $"age {Age(born, today)}{(pet.BirthDateEstimated ? " (estimated)" : string.Empty)}" : "age unknown",
            pet.IsDeceased ? "DECEASED" : null);

    /// <summary>"5 years 3 months", "7 months", or "under a month".</summary>
    public static string Age(DateOnly born, DateOnly today)
    {
        var months = ((today.Year - born.Year) * 12) + today.Month - born.Month - (today.Day < born.Day ? 1 : 0);

        if (months <= 0)
        {
            return "under a month";
        }

        var (years, rest) = (months / 12, months % 12);

        return years == 0 ? $"{rest} months" : rest == 0 ? $"{years} years" : $"{years} years {rest} months";
    }

    private static string Day(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Join(params string?[] parts) =>
        string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => Flatten(part!)));

    /// <summary>One line per record: newlines inside free text would let it pose as another record.</summary>
    private static string Flatten(string text) => string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();

    private static string Short(string text, int max) => text.Length <= max ? text : text[..max] + "...";
}
