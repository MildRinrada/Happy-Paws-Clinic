/** Drafting words for an owner: aftercare from a case, a reply in a thread. */
export const ownerMessageText = {
  aftercare: {
    title: 'ร่างคำแนะนำหลังรักษา',
    intro: 'AI เขียนคำแนะนำภาษาง่ายจากการวินิจฉัย การรักษา และยาของเคสนี้ ให้คัดลอกไปส่งเอง',
    run: 'ร่างคำแนะนำหลังรักษา',
    rerun: 'ร่างใหม่',
    textLabel: 'คำแนะนำ (แก้ได้ก่อนคัดลอก)',
  },
  reply: {
    run: 'ร่างคำตอบ',
    working: 'กำลังร่าง...',
    filled: 'ใส่ร่างจาก AI ในช่องตอบแล้ว ตรวจและแก้ก่อนกดส่ง',
  },
} as const;
