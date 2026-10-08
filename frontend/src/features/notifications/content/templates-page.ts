/** The template list page. */
export const templatesPage = {
  metaTitle: 'เทมเพลตข้อความ · Happy Paws Clinic',
  title: 'เทมเพลตข้อความ',
  subtitle: 'ถ้อยคำของข้อความแต่ละชนิดในแต่ละช่องทาง ระบบเติมค่าในวงเล็บปีกกาให้ตอนส่ง ส่วนในวงเล็บเหลี่ยมจะแสดงเมื่อมีค่าครบ',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูเทมเพลต',
  readOnly: 'แก้ถ้อยคำจากหน้านี้ยังไม่ได้ เพราะสัญญา API มีแค่การอ่านเทมเพลต',
  staffSendable: 'พนักงานส่งเองได้',
  systemOnly: 'ระบบส่งเอง',
  lineOff: 'ปิดทาง LINE',
  purposes: {
    service_messages: 'ข้อความเรื่องการดูแลสัตว์',
    marketing: 'การตลาด (ต้องได้รับความยินยอม)',
  } as Record<string, string>,
  variables: 'ค่าที่ใช้',
  optional: 'ไม่บังคับ',
  bodies: {
    line: 'LINE และอีเมล',
    sms: 'SMS',
  },
  smsLength: (length: number) => `${length} ตัวอักษรก่อนเติมค่า`,
};
