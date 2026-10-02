using HappyPaws.Domain.Consultations;

namespace HappyPaws.Application.Consultations.SeedTreatmentTemplates;

/// <summary>
/// Templates for <c>seed-treatment-templates</c>: enough common cases for the exam room's picker to
/// have something in it.
/// <para>
/// Doses are written as "ตามน้ำหนักตัว" on purpose. These are samples for a demo, not clinical
/// guidance, and a number here would be read as advice by someone who should be asking a vet.
/// </para>
/// </summary>
public static class SampleTreatmentTemplates
{
    public static IReadOnlyList<TreatmentTemplate> Create() =>
    [
        TreatmentTemplate.Create(
            "ถ่ายพยาธิและป้องกันเห็บหมัด",
            [new TemplateProcedure("ตรวจร่างกายทั่วไป")],
            [new TemplateMedicine("ยาถ่ายพยาธิชนิดเม็ด", "ตามน้ำหนักตัว", "ครั้งเดียว", 1, 1, "ให้หลังอาหาร")],
            species: "สุนัข",
            description: "ถ่ายพยาธิประจำ 3 เดือน",
            plan: "นัดถ่ายพยาธิครั้งถัดไปอีก 3 เดือน"),

        TreatmentTemplate.Create(
            "ผิวหนังอักเสบจากแบคทีเรีย",
            [new TemplateProcedure("ขูดตรวจผิวหนัง"), new TemplateProcedure("อาบน้ำยาฆ่าเชื้อ")],
            [
                new TemplateMedicine("Cephalexin", "ตามน้ำหนักตัว", "วันละ 2 ครั้ง หลังอาหาร", 14, 28),
                new TemplateMedicine("แชมพู Chlorhexidine 2%", "อาบทั้งตัว", "สัปดาห์ละ 2 ครั้ง", 14, 1, "ทิ้งไว้ 10 นาทีก่อนล้างออก"),
            ],
            species: "สุนัข",
            description: "ตุ่มหนอง ขนร่วงเป็นวง คัน",
            plan: "นัดติดตามผล 2 สัปดาห์ ถ้าไม่ดีขึ้นเพาะเชื้อ"),

        TreatmentTemplate.Create(
            "ท้องเสียเฉียบพลัน",
            [new TemplateProcedure("ตรวจอุจจาระ"), new TemplateProcedure("ให้สารน้ำใต้ผิวหนัง")],
            [new TemplateMedicine("โพรไบโอติกสำหรับสัตว์", "1 ซอง", "วันละครั้ง", 5, 5, "ผสมอาหาร")],
            description: "ถ่ายเหลวไม่เกิน 2 วัน ยังกินได้ ไม่ซึม",
            plan: "งดอาหาร 12 ชั่วโมงแล้วให้อาหารอ่อน ถ้ามีเลือดหรือซึมให้กลับมาทันที"),

        TreatmentTemplate.Create(
            "ทำหมันแมวเพศเมีย",
            [new TemplateProcedure("ดมยาสลบ"), new TemplateProcedure("ผ่าตัดทำหมัน")],
            [new TemplateMedicine("Meloxicam", "ตามน้ำหนักตัว", "วันละครั้ง หลังอาหาร", 3, 3)],
            species: "แมว",
            description: "งดน้ำงดอาหาร 8 ชั่วโมงก่อนผ่าตัด",
            plan: "ใส่ปลอกคอกันเลีย 10 วัน นัดตัดไหม 10 วัน"),

        TreatmentTemplate.Create(
            "วัคซีนลูกสุนัข 3 เข็ม",
            [new TemplateProcedure("ตรวจร่างกายก่อนฉีดวัคซีน"), new TemplateProcedure("ฉีดวัคซีนรวม")],
            [],
            species: "สุนัข",
            description: "เริ่มที่อายุ 6 ถึง 8 สัปดาห์",
            plan: "ฉีดเข็มละ 3 สัปดาห์ เข็มสุดท้ายพร้อมวัคซีนพิษสุนัขบ้า",
            sessions: 3,
            sessionIntervalDays: 21),

        TreatmentTemplate.Create(
            "คอร์สเคมีบำบัดทุก 3 สัปดาห์",
            [new TemplateProcedure("ตรวจเลือดก่อนให้ยา (CBC)"), new TemplateProcedure("ให้ยาเคมีบำบัดทางหลอดเลือด")],
            [],
            description: "ตรวจเลือดก่อนทุกครั้ง ถ้าเม็ดเลือดขาวต่ำให้เลื่อนรอบ",
            plan: "6 รอบ รอบละ 3 สัปดาห์ ประเมินผลหลังรอบที่ 3",
            sessions: 6,
            sessionIntervalDays: 21),
    ];
}
