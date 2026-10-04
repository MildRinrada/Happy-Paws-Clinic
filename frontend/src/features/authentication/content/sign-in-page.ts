/**
 * Everything the sign-in page says, and where its links go. Edit the wording here.
 *
 * `notices` are the short messages another page can leave on this one through `?notice=`. Only
 * the keys listed here are shown, so the address bar cannot be used to put any other text on the
 * page.
 */
export const signInPage = {
  metaTitle: 'เข้าสู่ระบบ',

  heading: 'ยินดีต้อนรับกลับมา',
  subheading: 'เข้าสู่ระบบเพื่อดูแลน้อง ๆ ของคุณ',

  identifierLabel: 'เบอร์โทรศัพท์หรืออีเมล',
  passwordLabel: 'รหัสผ่าน',
  forgotPassword: { label: 'ลืมรหัสผ่าน?', href: '/forgot-password' },
  submit: 'เข้าสู่ระบบ',
  submitting: 'กำลังตรวจสอบ',

  signUpPrompt: 'ยังไม่มีบัญชี?',
  signUp: { label: 'สมัครสมาชิก', href: '/register' },

  notices: {
    registered: 'สมัครสมาชิกเรียบร้อยแล้ว เข้าสู่ระบบด้วยเบอร์โทรและรหัสผ่านที่เพิ่งตั้ง',
  },

  secondFactor: {
    heading: 'ยืนยันตัวตนขั้นที่สอง',
    intro: 'กรอกรหัสจากแอป authenticator สำหรับ',
    codeLabel: 'รหัสจากแอป authenticator',
    codeHint: 'ตัวเลข 6 หลัก เปลี่ยนทุก 30 วินาที',
    submit: 'ยืนยัน',
    submitting: 'กำลังยืนยัน',
  },

  errors: {
    missing: 'กรุณากรอกเบอร์โทรศัพท์หรืออีเมล และรหัสผ่าน',
    badCode: 'รหัสยืนยันเป็นตัวเลข 6 หลักจากแอป authenticator',
    wrongCode: 'รหัสยืนยันไม่ถูกต้องหรือหมดอายุ ลองรหัสใหม่ หรือเริ่มเข้าสู่ระบบอีกครั้ง',
    tooMany: 'พยายามเข้าสู่ระบบบ่อยเกินไป กรุณารอสักครู่',
    unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  },
};
