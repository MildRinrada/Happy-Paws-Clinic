using HappyPaws.Domain.Access;
using HappyPaws.Domain.Staff;

namespace HappyPaws.Application.Staff.SeedStaff;

/// <summary>One sample member of staff.</summary>
/// <param name="EmailName">The part before <c>@</c> of their sign-in address.</param>
/// <param name="DisplayName">Name with title.</param>
/// <param name="StaffType">Position label.</param>
/// <param name="Department">A code from <see cref="Departments"/>.</param>
/// <param name="Role">Starter role name, or null for someone who does not use the system.</param>
/// <param name="LicenseNo">Professional licence, for vets.</param>
/// <param name="Profile">What the website says about them.</param>
public sealed record SampleStaffMember(
    string EmailName,
    string DisplayName,
    string StaffType,
    string Department,
    string? Role,
    string? LicenseNo,
    StaffPublicProfile Profile);

/// <summary>
/// Sample staff for <c>seed-staff</c>: enough people in every department for the team page to show
/// what it is for. Names and licence numbers are made up. Edit or add people here; a re-run adds
/// only addresses that are not taken yet.
/// </summary>
public static class SampleStaff
{
    /// <summary>Sample addresses use the reserved <c>.test</c> domain, so no mail can ever reach anyone.</summary>
    public const string EmailDomain = "staff.happypaws.test";

    public static readonly IReadOnlyList<SampleStaffMember> All =
    [
        new("kitti", "น.สพ. กิตติ วรกุล", "veterinarian", Departments.Veterinary, DefaultRoles.Veterinarian, "ส.พ. 04412",
            new("ผู้อำนวยการคลินิก", "ศัลยกรรมกระดูกและข้อ",
                "ดูแลงานผ่าตัดกระดูกและข้อของคลินิกมาตั้งแต่วันแรก เชื่อว่าการอธิบายให้เจ้าของเข้าใจสำคัญพอ ๆ กับการรักษา",
                null, 2006, true)),
        new("mint", "สพ.ญ. มินท์ ศรีสุข", "veterinarian", Departments.Veterinary, DefaultRoles.Veterinarian, "ส.พ. 06120",
            new("หัวหน้าสัตวแพทย์", "อายุรกรรมสัตว์เล็ก",
                "ชอบไขปริศนาโรคเรื้อรังของน้องหมาน้องแมว และเขียนบทความสุขภาพให้เจ้าของอ่านเข้าใจง่าย",
                null, 2015, true)),
        new("preeya", "สพ.ญ. ปรียา แก้วมณี", "veterinarian", Departments.Veterinary, DefaultRoles.Veterinarian, "ส.พ. 07385",
            new("สัตวแพทย์ประจำ", "ผิวหนังและภูมิแพ้",
                "ดูแลน้องที่คัน ขนร่วง และแพ้อาหาร ใจเย็นเป็นพิเศษกับน้องที่กลัวหมอ",
                null, 2017, true)),
        new("thanakorn", "น.สพ. ธนกร ศักดิ์ดี", "veterinarian", Departments.Veterinary, DefaultRoles.Veterinarian, "ส.พ. 08127",
            new("สัตวแพทย์ประจำ", "กระต่ายและสัตว์พิเศษ",
                "รักษากระต่าย หนูแกสบี้ และนก ถ้าน้องตัวเล็กของคุณไม่ค่อยกินอาหาร พามาคุยกันได้",
                null, 2019, true)),
        new("nicha", "สพ.ญ. ณิชา พงษ์ไพบูลย์", "veterinarian", Departments.Veterinary, DefaultRoles.Veterinarian, "ส.พ. 08544",
            new("สัตวแพทย์ประจำ", "ทันตกรรมสัตว์",
                "ขูดหินปูน ถอนฟัน และสอนเจ้าของแปรงฟันน้องที่บ้าน เพราะฟันดีคือสุขภาพดี",
                null, 2020, true)),

        new("kamonchanok", "กมลชนก ใจดี", "vet_assistant", Departments.Nursing, DefaultRoles.VetAssistant, null,
            new("หัวหน้าผู้ช่วยสัตวแพทย์", "ดูแลสัตว์ป่วยใน",
                "ดูแลน้องที่นอนโรงพยาบาลทั้งกลางวันและกลางคืน และโทรเล่าอาการให้เจ้าของฟังทุกวัน",
                null, 2012, true)),
        new("orauma", "อรอุมา บุญมา", "vet_assistant", Departments.Nursing, DefaultRoles.VetAssistant, null,
            new("ผู้ช่วยสัตวแพทย์ห้องผ่าตัด", "เตรียมผ่าตัดและดูแลหลังผ่าตัด",
                "เตรียมห้องผ่าตัดและอยู่ข้างน้องจนตื่นจากยาสลบ",
                null, 2018, true)),
        new("worawut", "วรวุฒิ แสงทอง", "vet_assistant", Departments.Nursing, DefaultRoles.VetAssistant, null,
            new("ผู้ช่วยสัตวแพทย์", "เจาะเลือดและงานห้องแล็บ",
                "มือเบาเวลาเจาะเลือด และรู้ว่าขนมชิ้นไหนช่วยให้น้องหายกลัว",
                null, 2021, true)),

        new("siriporn", "ศิริพร มีสุข", "receptionist", Departments.FrontDesk, DefaultRoles.Receptionist, null,
            new("หัวหน้าเคาน์เตอร์ต้อนรับ", "นัดหมายและติดตามวัคซีน",
                "จำชื่อน้องได้แทบทุกตัว และเป็นคนโทรเตือนเมื่อถึงเวลาฉีดวัคซีนเข็มต่อไป",
                null, 2016, true)),

        // Not listed on the website: being on the staff is not agreeing to be on the website.
        new("panupong", "ภานุพงศ์ ทองดี", "receptionist", Departments.FrontDesk, DefaultRoles.Receptionist, null,
            new("เจ้าหน้าที่ต้อนรับ", null, null, null, 2022, false)),

        new("janpen", "จันทร์เพ็ญ รุ่งเรือง", "cashier", Departments.Finance, DefaultRoles.Cashier, null,
            new("เจ้าหน้าที่การเงิน", "ใบเสร็จและเคลมประกัน",
                "ช่วยอธิบายค่าใช้จ่ายก่อนเริ่มรักษา และเตรียมเอกสารเคลมประกันให้ครบ",
                null, 2014, true)),

        new("nattawut", "ณัฐวุฒิ ปัญญาดี", "clinic_admin", Departments.Management, DefaultRoles.ClinicAdmin, null,
            new("ผู้จัดการคลินิก", "งานบริหารและคุณภาพบริการ",
                "ดูแลให้ทุกการมาคลินิกเป็นไปอย่างราบรื่น ตั้งแต่ที่จอดรถจนถึงการติดตามหลังกลับบ้าน",
                null, 2010, true)),

        new("somchai", "สมชาย ขยันงาน", "housekeeping", Departments.Support, null, null,
            new("เจ้าหน้าที่ความสะอาดและฆ่าเชื้อ", "ห้องผ่าตัดและกรงพักฟื้น",
                "ทำให้ทุกห้องสะอาดและปลอดเชื้อก่อนน้องตัวต่อไปจะเข้ามา",
                null, 2017, true)),
        new("piyanuch", "ปิยนุช ศรีงาม", "animal_caretaker", Departments.Support, null, null,
            new("ผู้ดูแลสัตว์พักฟื้นและฝากเลี้ยง", "ฝากเลี้ยงและพาเดิน",
                "ให้อาหาร พาเดินเล่น และส่งรูปน้องให้เจ้าของดูระหว่างฝากเลี้ยง",
                null, 2020, true)),
    ];

    public static string EmailOf(SampleStaffMember member) => $"{member.EmailName}@{EmailDomain}";
}
