/** Ordering a lab test from the consulting room. */
export const labOrder = {
  none: 'ยังไม่มีคำขอตรวจแล็บในเคสนี้',
  cannotRead: 'บัญชีนี้ดูผลแล็บไม่ได้ (lab:read) คำขอที่ส่งแล้วจึงไม่แสดงที่นี่',
  worklist: 'ไปหน้างานแล็บ',

  tests: 'รายการตรวจ',
  testName: 'ชื่อการตรวจ',
  testPlaceholder: 'เช่น CBC',
  testCode: 'รหัส (ไม่บังคับ)',
  addTest: 'เพิ่มรายการตรวจ',
  removeTest: 'ลบ',
  test: (index: number) => `รายการที่ ${index}`,
  noTests: 'ใส่ชื่อการตรวจอย่างน้อยหนึ่งรายการ',

  destination: 'ส่งที่',
  inHouse: 'แล็บในคลินิก',
  external: 'แล็บภายนอก',
  externalName: 'ชื่อแล็บภายนอก',
  urgent: 'ด่วน',
  notes: 'ข้อมูลถึงแล็บ (ไม่บังคับ)',
  notesPlaceholder: 'เช่น งดอาหาร 12 ชั่วโมง สงสัยโรคไต',

  submit: 'ส่งตรวจ',
  working: 'กำลังส่ง…',
  sent: 'ส่งตรวจแล็บแล้ว',
};
