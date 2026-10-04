/** The settings page for booking, queue numbering and medical-record rules. */
export const rulesSettings = {
  metaTitle: 'กติกาการจอง คิว และเวชระเบียน · Happy Paws Clinic',
  title: 'กติกาการจอง คิว และเวชระเบียน',
  subtitle: 'กติกาที่มีผลกับนัดใหม่ บัตรคิวใบใหม่ และเคสที่ปิดหลังบันทึก ของเดิมไม่เปลี่ยน',

  booking: {
    heading: 'การจองนัด',
    slotMinutes: 'ความยาวช่องเวลา',
    slotMinutesHint: 'ช่วงห่างของเวลาเริ่มนัดที่ให้เลือก',
    minutes: (value: number) => `${value} นาที`,
    leadTimeMinutes: 'ต้องจองล่วงหน้าอย่างน้อย (นาที)',
    leadTimeMinutesHint: 'เช่น 60 คือจองเวลาที่เหลือไม่ถึงหนึ่งชั่วโมงไม่ได้',
    cancellationCutoffHours: 'ลูกค้ายกเลิกเองได้ก่อนนัด (ชั่วโมง)',
    cancellationCutoffHoursHint: 'เลยเวลานี้แล้วลูกค้าต้องโทรหาคลินิก',
    maxAdvanceDays: 'จองล่วงหน้าได้ไม่เกิน (วัน)',
    walkInSlotsPerHour: 'โควตา walk-in ต่อชั่วโมง',
    walkInSlotsPerHourHint: 'ระบบเก็บค่านี้ไว้ แต่ยังไม่ได้ใช้กันช่องเวลาออกจากการจองออนไลน์',
  },

  queue: {
    heading: 'เลขคิว',
    prefix: 'ตัวอักษรนำหน้า',
    prefixHint: 'A-Z ไม่เกิน 3 ตัว เว้นว่างได้',
    digits: 'จำนวนหลัก',
    resetDaily: 'เริ่มนับใหม่ทุกวัน',
    example: 'ตัวอย่างเลขคิวที่ 14 ของวัน',
  },

  record: {
    heading: 'เวชระเบียน',
    auditNote: 'การแก้ส่วนนี้ถูกบันทึกใน audit log',
    requiredFields: 'ส่วนที่ต้องกรอกก่อนปิดเคส',
    requiredFieldsHint: 'การวินิจฉัยต้องมีทุกเคสอยู่แล้ว ไม่ต้องเลือก',
    examinationSystems: 'ระบบในแบบฟอร์มตรวจร่างกาย',
    examinationSystemsHint: 'หนึ่งบรรทัดต่อหนึ่งระบบ ไม่เกิน 40 ระบบ',
    ownerVisibleSections: 'ส่วนที่เจ้าของเห็นใน portal',
    signerMustBeCaseVet: 'ผู้เซ็นเวชระเบียนต้องเป็นหมอเจ้าของเคสเท่านั้น',
  },

  /** `required_fields`, in the backend's order. */
  requiredFieldLabels: {
    chief_complaint: 'อาการสำคัญ',
    subjective: 'ข้อมูลจากเจ้าของ (S)',
    objective: 'สิ่งที่ตรวจพบ (O)',
    assessment: 'การประเมิน (A)',
    plan: 'แผนการรักษา (P)',
    vitals: 'สัญญาณชีพ',
    examination: 'ผลตรวจร่างกาย',
  } as Record<string, string>,

  /** `owner_visible_sections`, in the backend's order. */
  sectionLabels: {
    vitals: 'สัญญาณชีพ',
    examination: 'ผลตรวจร่างกาย',
    diagnosis: 'การวินิจฉัย',
    treatment: 'การรักษา',
    prescription: 'ใบสั่งยา',
    lab_result: 'ผลแล็บ',
    attachments: 'ไฟล์แนบ',
  } as Record<string, string>,
};

/** The slot lengths offered: multiples of five that divide an hour or a working morning evenly. */
export const SLOT_CHOICES = [5, 10, 15, 20, 30, 45, 60, 90, 120];
