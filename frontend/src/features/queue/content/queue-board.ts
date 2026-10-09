/** The queue board's links and the per-ticket controls added beside call and skip. */
export const queueBoard = {
  links: {
    history: 'ประวัติคิว',
    display: 'เปิดจอห้องรอ',
    displayHint: 'เปิดในแท็บใหม่ สำหรับจอทีวีในห้องรอ',
  },

  messages: {
    conflict: 'มีคนเปลี่ยนบัตรนี้ไปก่อนแล้ว กระดานโหลดใหม่ให้แล้ว ลองอีกครั้ง',
    unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
    cancel: 'ยกเลิก',
  },

  vet: {
    label: (number: string) => `สัตวแพทย์ของคิว ${number}`,
    placeholder: 'จ่ายหมอ',
  },

  room: {
    label: (number: string) => `ห้องตรวจของคิว ${number}`,
    placeholder: 'เลือกห้อง',
  },

  priority: {
    trigger: 'ความเร่งด่วน',
    title: (number: string) => `เปลี่ยนความเร่งด่วนของคิว ${number}`,
    field: 'ความเร่งด่วน',
    options: { normal: 'ตามลำดับ', urgent: 'เร่งด่วน', emergency: 'ฉุกเฉิน' },
    reason: 'เหตุผล',
    reasonHint: 'ต้องมีเมื่อไม่ใช่ตามลำดับ บันทึกใน audit log พร้อมค่าเดิมและค่าใหม่',
    submit: 'บันทึก',
    submitting: 'กำลังบันทึก…',
  },

  leave: {
    trigger: 'ออกจากคิว',
    title: (number: string) => `เอาคิว ${number} ออกจากคิว`,
    body: 'บัตรจะเป็น "ออกจากคิว" และหายจากกระดาน ไม่นับเป็นการตรวจ ดูย้อนหลังได้ในประวัติคิว',
    reason: 'เหตุผล (ไม่บังคับ)',
    reasonHint: 'เช่น เจ้าของกลับบ้านแล้ว หรือยกเลิกนัด เก็บไว้บนบัตร',
    submit: 'ออกจากคิว',
    submitting: 'กำลังบันทึก…',
  },
} as const;
