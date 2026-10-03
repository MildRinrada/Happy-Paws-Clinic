/** The counter's "new customer" form, including the duplicate-phone stop. */
export const registerCustomer = {
  metaTitle: 'ลงทะเบียนลูกค้าใหม่ · Happy Paws Clinic',
  title: 'ลงทะเบียนลูกค้าใหม่',
  subtitle: 'ใส่ข้อมูลเจ้าของก่อน แล้วเพิ่มสัตว์เลี้ยงในหน้าถัดไป',
  back: 'กลับไปหน้าค้นหา',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ลงทะเบียนลูกค้า (customer:write)',
  fields: {
    displayName: 'ชื่อเจ้าของ',
    displayNameHint: 'ชื่อที่ลูกค้าบอก เช่น คุณสมชาย ใจดี',
    phone: 'เบอร์โทร',
    phoneHint: 'ใช้ส่งข้อความแจ้งเตือน และใช้ตรวจว่าเคยลงทะเบียนแล้วหรือยัง',
    email: 'อีเมล (ไม่บังคับ)',
  },
  submit: 'บันทึกลูกค้า',
  submitting: 'กำลังบันทึก…',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  duplicate: {
    title: 'เบอร์นี้มีลูกค้าใช้อยู่แล้ว',
    body: 'ถ้าเป็นคนเดียวกัน ให้เปิดเรกคอร์ดเดิมแทนการสร้างใหม่ ถ้าเป็นคนในบ้านเดียวกันที่ใช้เบอร์ร่วมกัน สร้างใหม่ได้',
    open: 'เปิดเรกคอร์ดนี้',
    createAnyway: 'คนละคน สร้างลูกค้าใหม่',
  },
};
