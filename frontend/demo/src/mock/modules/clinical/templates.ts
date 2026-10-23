import type { TreatmentTemplate } from '@/features/prescriptions/types/treatment-template.types';

/**
 * The clinic's treatment templates: the backend's `SampleTreatmentTemplates`, as its
 * `seed-treatment-templates` command plants them, plus a few the demo clinic would keep.
 * Doses read "ตามน้ำหนักตัว" on purpose, as there: samples, not clinical guidance.
 */

type Template = Omit<TreatmentTemplate, 'id'>;

const procedure = (name: string, quantity = 1) => ({ name, quantity });

export const TREATMENT_TEMPLATES: Template[] = [
  {
    name: 'ถ่ายพยาธิและป้องกันเห็บหมัด',
    species: 'สุนัข',
    description: 'ถ่ายพยาธิประจำ 3 เดือน',
    procedures: [procedure('ตรวจร่างกายทั่วไป')],
    medicines: [{ drug_name: 'ยาถ่ายพยาธิชนิดเม็ด', dose: 'ตามน้ำหนักตัว', frequency: 'ครั้งเดียว', duration_days: 1, quantity: 1, instructions: 'ให้หลังอาหาร' }],
    plan: 'นัดถ่ายพยาธิครั้งถัดไปอีก 3 เดือน',
  },
  {
    name: 'ผิวหนังอักเสบจากแบคทีเรีย',
    species: 'สุนัข',
    description: 'ตุ่มหนอง ขนร่วงเป็นวง คัน',
    procedures: [procedure('ขูดตรวจผิวหนัง'), procedure('อาบน้ำยาฆ่าเชื้อ')],
    medicines: [
      { drug_name: 'Cephalexin', dose: 'ตามน้ำหนักตัว', frequency: 'วันละ 2 ครั้ง หลังอาหาร', duration_days: 14, quantity: 28 },
      { drug_name: 'แชมพู Chlorhexidine 2%', dose: 'อาบทั้งตัว', frequency: 'สัปดาห์ละ 2 ครั้ง', duration_days: 14, quantity: 1, instructions: 'ทิ้งไว้ 10 นาทีก่อนล้างออก' },
    ],
    plan: 'นัดติดตามผล 2 สัปดาห์ ถ้าไม่ดีขึ้นเพาะเชื้อ',
  },
  {
    name: 'ท้องเสียเฉียบพลัน',
    description: 'ถ่ายเหลวไม่เกิน 2 วัน ยังกินได้ ไม่ซึม',
    procedures: [procedure('ตรวจอุจจาระ'), procedure('ให้สารน้ำใต้ผิวหนัง')],
    medicines: [{ drug_name: 'โพรไบโอติกสำหรับสัตว์', dose: '1 ซอง', frequency: 'วันละครั้ง', duration_days: 5, quantity: 5, instructions: 'ผสมอาหาร' }],
    plan: 'งดอาหาร 12 ชั่วโมงแล้วให้อาหารอ่อน ถ้ามีเลือดหรือซึมให้กลับมาทันที',
  },
  {
    name: 'ทำหมันแมวเพศเมีย',
    species: 'แมว',
    description: 'งดน้ำงดอาหาร 8 ชั่วโมงก่อนผ่าตัด',
    procedures: [procedure('ดมยาสลบ'), procedure('ผ่าตัดทำหมัน')],
    medicines: [{ drug_name: 'Meloxicam', dose: 'ตามน้ำหนักตัว', frequency: 'วันละครั้ง หลังอาหาร', duration_days: 3, quantity: 3 }],
    plan: 'ใส่ปลอกคอกันเลีย 10 วัน นัดตัดไหม 10 วัน',
  },
  {
    name: 'วัคซีนลูกสุนัข 3 เข็ม',
    species: 'สุนัข',
    description: 'เริ่มที่อายุ 6 ถึง 8 สัปดาห์',
    procedures: [procedure('ตรวจร่างกายก่อนฉีดวัคซีน'), procedure('ฉีดวัคซีนรวม')],
    medicines: [],
    plan: 'ฉีดเข็มละ 3 สัปดาห์ เข็มสุดท้ายพร้อมวัคซีนพิษสุนัขบ้า',
    sessions: 3,
    session_interval_days: 21,
  },
  {
    name: 'คอร์สเคมีบำบัดทุก 3 สัปดาห์',
    description: 'ตรวจเลือดก่อนทุกครั้ง ถ้าเม็ดเลือดขาวต่ำให้เลื่อนรอบ',
    procedures: [procedure('ตรวจเลือดก่อนให้ยา (CBC)'), procedure('ให้ยาเคมีบำบัดทางหลอดเลือด')],
    medicines: [],
    plan: '6 รอบ รอบละ 3 สัปดาห์ ประเมินผลหลังรอบที่ 3',
    sessions: 6,
    session_interval_days: 21,
  },
  {
    name: 'หูอักเสบจากยีสต์',
    species: 'สุนัข',
    description: 'สะบัดหัว เกาหู มีขี้หูสีน้ำตาลกลิ่นเหม็น',
    procedures: [procedure('ตรวจขี้หูด้วยกล้องจุลทรรศน์'), procedure('ล้างหู')],
    medicines: [
      { drug_name: 'ยาหยอดหู Otomax', dose: '4 หยด', frequency: 'วันละ 2 ครั้ง', duration_days: 10, quantity: 1, instructions: 'นวดโคนหูเบา ๆ หลังหยอด' },
      { drug_name: 'น้ำยาล้างหู Epi-Otic', dose: 'เติมให้เต็มช่องหู', frequency: 'วันเว้นวัน', duration_days: 10, quantity: 1 },
    ],
    plan: 'นัดตรวจหูซ้ำ 10 วัน',
  },
  {
    name: 'ขูดหินปูนและขัดฟัน',
    description: 'ตรวจเลือดก่อนดมยาสำหรับสัตว์อายุเกิน 7 ปี',
    procedures: [procedure('ดมยาสลบ'), procedure('ขูดหินปูนด้วยเครื่องอัลตราโซนิก'), procedure('ขัดฟัน')],
    medicines: [{ drug_name: 'Amoxicillin-Clavulanate', dose: 'ตามน้ำหนักตัว', frequency: 'วันละ 2 ครั้ง หลังอาหาร', duration_days: 7, quantity: 14 }],
    plan: 'ให้อาหารนิ่ม 3 วัน แปรงฟันทุกวัน นัดตรวจช่องปากทุก 6 เดือน',
  },
  {
    name: 'แมวไข้หวัดทางเดินหายใจส่วนบน',
    species: 'แมว',
    description: 'จาม น้ำมูก ตาแฉะ เบื่ออาหาร',
    procedures: [procedure('ตรวจร่างกายทั่วไป'), procedure('พ่นละอองยา')],
    medicines: [
      { drug_name: 'Doxycycline', dose: 'ตามน้ำหนักตัว', frequency: 'วันละครั้ง ตามด้วยน้ำ', duration_days: 7, quantity: 7 },
      { drug_name: 'L-Lysine เจล', dose: '1 ช้อนชา', frequency: 'วันละ 2 ครั้ง', duration_days: 14, quantity: 1 },
    ],
    plan: 'เช็ดตาและจมูกด้วยน้ำเกลือ ถ้าไม่กินอาหารเกิน 2 วันให้กลับมา',
  },
];
