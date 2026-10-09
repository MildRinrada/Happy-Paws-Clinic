/** The list of webhooks and what can be done to each (docs/contract-extensions.md § 22). */
export const webhookListText = {
  heading: 'webhook ทั้งหมด',
  empty: 'ยังไม่มี webhook',
  emptyHint: 'สร้าง webhook ด้านบน แล้วจะขึ้นในรายการนี้',
  noDescription: 'ไม่มีคำอธิบาย',
  created: (when: string) => `สร้างเมื่อ ${when}`,
  log: 'ประวัติการส่ง',
  edit: 'แก้ไข',
  back: 'กลับ',

  status: {
    active: 'เปิดใช้งาน',
    inactive: 'ปิดอยู่',
  },

  toggle: {
    enable: 'เปิดใช้งาน',
    disable: 'ปิดใช้งาน',
    confirm: 'ปิดแล้ว รายการที่รอส่งจะหยุดทันที และเหตุการณ์ที่เกิดระหว่างปิดจะไม่ถูกส่งตามหลัง',
    confirmButton: 'ยืนยันปิดใช้งาน',
    pending: 'กำลังบันทึก…',
  },

  rotate: {
    button: 'สร้าง secret ใหม่',
    confirm: 'secret เดิมใช้ไม่ได้ทันที ระบบปลายทางต้องเปลี่ยนเป็นค่าใหม่ การส่งที่ถูกปฏิเสธระหว่างนั้นจะลองใหม่ตามรอบปกติ',
    confirmButton: 'ยืนยันสร้าง secret ใหม่',
    pending: 'กำลังสร้าง…',
  },

  rotated: {
    heading: 'secret ใหม่',
    secret: 'Signing secret',
    warning: 'คัดลอก secret ใหม่ไปใส่ในระบบปลายทางตอนนี้ ระบบจะไม่แสดงค่านี้อีก และ secret เดิมใช้ไม่ได้แล้ว',
    done: 'เก็บแล้ว',
  },

  remove: {
    button: 'ลบ',
    confirm: 'ลบแล้ว webhook นี้จะไม่ส่งอะไรอีกและกู้คืนไม่ได้ ประวัติการส่งยังเปิดดูได้อีก 30 วัน',
    confirmButton: 'ยืนยันลบ',
    pending: 'กำลังลบ…',
  },

  editForm: {
    heading: 'แก้ไข webhook',
    descriptionHint: 'เว้นว่างเพื่อลบคำอธิบาย',
    urlHint: 'ใช้กติกาเดียวกับตอนสร้าง การส่งครั้งถัดไปทุกครั้งจะไปที่ URL ใหม่ รวมการลองใหม่ของเหตุการณ์ก่อนหน้า',
  },
};
