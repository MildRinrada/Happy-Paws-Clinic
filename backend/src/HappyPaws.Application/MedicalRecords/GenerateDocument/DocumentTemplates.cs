using HappyPaws.Application.Patients.ListVaccinations;

namespace HappyPaws.Application.MedicalRecords.GenerateDocument;

/// <summary>
/// The three documents the contract names — "ใบรับรองสุขภาพ ใบส่งตัว สมุดวัคซีน" — as content.
/// Wording only; how the page looks is the renderer's.
/// </summary>
public static class DocumentTemplates
{
    public static PrintableDocument HealthCertificate(DocumentContext context, string? remarks, string reference)
    {
        var examinedOn = context.Visit is { } visit ? context.ThaiDate(visit.OpenedAt) : DocumentContext.ThaiDate(context.Today);
        var current = ListVaccinationsHandler.DueFrom(context.Vaccinations, context.Today);

        return new PrintableDocument(
            "ใบรับรองสุขภาพสัตว์",
            context.Clinic.Name,
            context.ClinicLines,
            [
                context.PatientSection(),
                new("ผลการตรวจ", [new("วันที่ตรวจ", examinedOn), new("น้ำหนักล่าสุด", context.Weight)]),
                new(
                    "การรับรอง",
                    Paragraph: string.IsNullOrWhiteSpace(remarks)
                        ? $"ข้าพเจ้าได้ตรวจร่างกายสัตว์ข้างต้นเมื่อวันที่ {examinedOn} ไม่พบอาการของโรคติดต่อในวันที่ตรวจ"
                        : remarks.Trim()),
                new(
                    "วัคซีนที่ยังคุ้มกัน",
                    Table: new PrintableTable(
                        ["วัคซีน", "ฉีดล่าสุด", "ครบกำหนดครั้งถัดไป"],
                        [.. current
                            .Where(item => item.Status != "overdue")
                            .Select(item => (IReadOnlyList<string>)[item.VaccineName, context.ThaiDate(item.LastGivenAt), DocumentContext.ThaiDate(item.NextDueOn)])])),
                AllergySection(context),
            ],
            context.Signer,
            reference,
            DocumentContext.ThaiDate(context.Today));
    }

    public static PrintableDocument ReferralLetter(DocumentContext context, string referTo, string reason, string reference)
    {
        var visit = context.Visit!;

        return new PrintableDocument(
            "ใบส่งตัวผู้ป่วย",
            context.Clinic.Name,
            context.ClinicLines,
            [
                new("ถึง", [new("สถานพยาบาลปลายทาง", referTo.Trim()), new("เหตุผลที่ส่งตัว", reason.Trim())]),
                context.PatientSection(),
                new(
                    "การตรวจครั้งล่าสุด",
                    [
                        new("วันที่ตรวจ", context.ThaiDate(visit.OpenedAt)),
                        new("อาการที่มา", visit.ChiefComplaint ?? "-"),
                        new("น้ำหนักล่าสุด", context.Weight),
                        new("การประเมิน", visit.Assessment ?? "-"),
                    ]),
                new("การวินิจฉัย", Paragraph: visit.Diagnoses.Count == 0 ? "-" : string.Join(", ", visit.Diagnoses.Select(d => d.Description))),
                new(
                    "การรักษาที่ได้รับ",
                    Table: new PrintableTable(
                        ["รายการ", "จำนวน"],
                        [.. visit.Treatments.Select(t => (IReadOnlyList<string>)[t.Name, t.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)])])),
                new(
                    "ยาที่ได้รับ",
                    Table: new PrintableTable(
                        ["ยา", "ขนาด", "วิธีใช้", "จำนวนวัน"],
                        [.. visit.Prescriptions.Select(p => (IReadOnlyList<string>)[p.DrugName, p.Dose, p.Frequency, p.DurationDays.ToString(System.Globalization.CultureInfo.InvariantCulture)])])),
                AllergySection(context),
                new("แผนการรักษาต่อ", Paragraph: visit.Plan ?? "-"),
            ],
            context.Signer,
            reference,
            DocumentContext.ThaiDate(context.Today));
    }

    public static PrintableDocument VaccinationBook(DocumentContext context, string reference) =>
        new(
            "สมุดบันทึกการฉีดวัคซีน",
            context.Clinic.Name,
            context.ClinicLines,
            [
                context.PatientSection(),
                new(
                    "ประวัติการฉีด",
                    Table: new PrintableTable(
                        ["วันที่ฉีด", "วัคซีน", "ล็อต", "ครบกำหนดครั้งถัดไป"],
                        [.. context.Vaccinations
                            .OrderBy(dose => dose.AdministeredAt)
                            .Select(dose => (IReadOnlyList<string>)
                            [
                                context.ThaiDate(dose.AdministeredAt),
                                dose.VaccineName,
                                dose.LotNumber,
                                dose.NextDueOn is { } due ? DocumentContext.ThaiDate(due) : "-",
                            ])])),
            ],
            context.Signer,
            reference,
            DocumentContext.ThaiDate(context.Today));

    private static PrintableSection AllergySection(DocumentContext context) =>
        new("ประวัติแพ้ยา", Paragraph: context.Allergies.Count == 0
            ? "ไม่มีประวัติแพ้ที่บันทึกไว้"
            : string.Join(", ", context.Allergies.Select(allergy => $"{allergy.Substance} ({allergy.Severity.ToString().ToLowerInvariant()})")));
}
