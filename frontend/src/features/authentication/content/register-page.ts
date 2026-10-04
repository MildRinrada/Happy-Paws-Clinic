/**
 * Everything the sign-up page says. Edit the wording here.
 *
 * Messages the backend sends (a wrong code, a number that already has an account, too many codes)
 * are shown as the backend wrote them, so they are not here.
 */
export const registerPage = {
  metaTitle: 'สมัครสมาชิก',

  heading: 'สมัครสมาชิก',
  subheading: 'ดูคิว นัดหมาย และประวัติของน้อง ๆ ได้จากมือถือ',

  phone: {
    label: 'เบอร์โทรศัพท์',
    hint: 'คลินิกจะส่งรหัส 6 หลักทาง SMS เพื่อยืนยันว่าเป็นเบอร์ของคุณ',
    placeholder: '081-234-5678',
    submit: 'ส่งรหัสยืนยัน',
    submitting: 'กำลังส่งรหัส',
  },

  codeSent: {
    sentTo: 'ส่งรหัสไปที่',
    validUntil: (time: string) => `รหัสใช้ได้ถึง ${time} น.`,
    changeNumber: 'เปลี่ยนเบอร์',
    resend: 'ส่งรหัสอีกครั้ง',
    resendIn: (seconds: number) => `ส่งรหัสอีกครั้งได้ใน ${seconds} วินาที`,
    resending: 'กำลังส่งรหัส',
    resent: 'ส่งรหัสใหม่แล้ว ใช้รหัสล่าสุดเท่านั้น',
  },

  details: {
    codeLabel: 'รหัสยืนยันจาก SMS',
    codeHint: 'ตัวเลข 6 หลัก',
    nameLabel: 'ชื่อที่ให้คลินิกเรียก',
    namePlaceholder: 'เช่น คุณสมใจ',
    emailLabel: 'อีเมล (ไม่บังคับ)',
    emailHint: 'ถ้าใส่ไว้ ลิงก์ตั้งรหัสผ่านใหม่จะส่งไปที่อีเมลนี้',
    passwordLabel: 'ตั้งรหัสผ่าน',
    passwordHint: 'อย่างน้อย 8 ตัวอักษร ใช้คู่กับเบอร์โทรเพื่อเข้าสู่ระบบครั้งถัดไป',
    confirmLabel: 'พิมพ์รหัสผ่านอีกครั้ง',
    submit: 'สมัครสมาชิก',
    submitting: 'กำลังสมัคร',
  },

  privacyNote: {
    before: 'เมื่อสมัคร คุณรับทราบ',
    link: { label: 'นโยบายความเป็นส่วนตัว', href: '/privacy' },
    after: 'ของคลินิก',
  },

  signInPrompt: 'มีบัญชีอยู่แล้ว?',
  signIn: { label: 'เข้าสู่ระบบ', href: '/login' },

  errors: {
    badPhone: 'เบอร์โทรต้องมี 9 ถึง 15 หลัก',
    badCode: 'รหัสยืนยันเป็นตัวเลข 6 หลัก',
    missingName: 'กรุณากรอกชื่อ',
    nameTooLong: 'ชื่อยาวได้ไม่เกิน 200 ตัวอักษร',
    badEmail: 'รูปแบบอีเมลไม่ถูกต้อง',
    passwordTooShort: 'รหัสผ่านต้องยาวอย่างน้อย 8 ตัวอักษร',
    passwordTooLong: 'รหัสผ่านยาวได้ไม่เกิน 256 ตัวอักษร',
    mismatch: 'รหัสผ่านทั้งสองช่องไม่ตรงกัน',
    unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  },
};
