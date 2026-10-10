/** The /ai page: ask the clinic's numbers, and this month's allowance. */
export const askPage = {
  metaTitle: 'ผู้ช่วย AI',
  title: 'ผู้ช่วย AI',
  subtitle: 'ถามตัวเลขของคลินิกจากรายงานที่บัญชีนี้ดูได้ AI ไม่เห็นข้อมูลเกินสิทธิ์ของคุณ',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ใช้ผู้ช่วย AI (ต้องมีสิทธิ์ ai:use)',
  disabled: 'ผู้ช่วย AI ยังไม่เปิดใช้ในระบบนี้',
  greeting: 'สวัสดีค่ะ ถามเรื่องนัด คิว รายได้ บริการ หรือสต๊อกได้เลย ตอบจากรายงานที่บัญชีนี้ดูได้เท่านั้น',
  suggestions: ['วันนี้มีนัดกี่ราย', 'สัปดาห์นี้คิวรอนานแค่ไหน', 'มีสต๊อกอะไรต้องสั่งเพิ่ม', 'เดือนนี้บริการไหนทำเงินมากที่สุด'],
  placeholder: 'พิมพ์คำถาม',
  send: 'ส่ง',
  restart: 'เริ่มใหม่',
  you: 'คุณ',
  assistant: 'ผู้ช่วย AI',
  notStored: 'บทสนทนาไม่ถูกเก็บในระบบ หายเมื่อออกจากหน้านี้',
  usage: {
    title: 'การใช้งานเดือนนี้',
    used: (used: number, limit: number) => `ใช้ไป ${used.toLocaleString('th-TH')} จาก ${limit.toLocaleString('th-TH')} ครั้ง`,
    month: (month: string) => `เดือน ${month}`,
    note: 'โควตารายเดือนตั้งโดยผู้ดูแลระบบ เริ่มนับใหม่ทุกต้นเดือน ข้อมูลที่ดึงจากแคชไม่นับ',
    features: {
      pet_summary: 'สรุปประวัติสัตว์',
      soap_draft: 'ร่างบันทึกการตรวจ',
      owner_message: 'ร่างข้อความถึงเจ้าของ',
      ask: 'ถามตัวเลข',
      daily_brief: 'สรุปประจำวัน',
    } as Record<string, string>,
  },
} as const;
