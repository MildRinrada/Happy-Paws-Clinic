/**
 * Everything the forgot-password and reset-password pages say. Edit the wording here.
 *
 * The confirmation after asking for a link is the same whether or not the email or number has an
 * account. The backend answers the same way for both, on purpose: a different message would tell a
 * stranger who is a customer here. Keep it that way when rewording.
 */
export const forgotPasswordPage = {
  metaTitle: 'ลืมรหัสผ่าน',

  heading: 'ลืมรหัสผ่าน',
  subheading: 'กรอกอีเมลหรือเบอร์โทรที่ใช้สมัคร แล้วเราจะส่งลิงก์ตั้งรหัสผ่านใหม่ให้',

  identifierLabel: 'อีเมลหรือเบอร์โทรศัพท์',
  submit: 'ส่งลิงก์ตั้งรหัสผ่านใหม่',
  submitting: 'กำลังส่ง',

  sent: {
    title: 'ตรวจอีเมลหรือ SMS ของคุณ',
    body: 'ถ้าอีเมลหรือเบอร์นี้มีบัญชีอยู่ เราได้ส่งลิงก์ตั้งรหัสผ่านใหม่ไปแล้ว ลิงก์ไปทางอีเมลถ้าบัญชีมีอีเมล ไม่อย่างนั้นไปทาง SMS',
    expiry: 'ลิงก์ใช้ได้ 30 นาทีและใช้ได้ครั้งเดียว ถ้าไม่ได้รับภายในไม่กี่นาที ลองตรวจกล่องจดหมายขยะ หรือขอใหม่อีกครั้ง',
    again: 'ขอลิงก์อีกครั้ง',
  },

  errors: {
    missing: 'กรุณากรอกอีเมลหรือเบอร์โทรศัพท์',
    unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  },
};

export const resetPasswordPage = {
  metaTitle: 'ตั้งรหัสผ่านใหม่',

  heading: 'ตั้งรหัสผ่านใหม่',
  subheading: 'ตั้งรหัสผ่านใหม่สำหรับบัญชีของคุณ',

  reading: 'กำลังเปิดลิงก์',

  passwordLabel: 'รหัสผ่านใหม่',
  passwordHint: 'อย่างน้อย 8 ตัวอักษร',
  confirmLabel: 'พิมพ์รหัสผ่านใหม่อีกครั้ง',
  submit: 'บันทึกรหัสผ่านใหม่',
  submitting: 'กำลังบันทึก',
  signOutNote: 'เมื่อบันทึกแล้ว ทุกอุปกรณ์ที่เข้าสู่ระบบไว้จะถูกออกจากระบบ',

  done: {
    title: 'ตั้งรหัสผ่านใหม่แล้ว',
    body: 'เข้าสู่ระบบด้วยรหัสผ่านใหม่ได้เลย',
    signIn: { label: 'ไปหน้าเข้าสู่ระบบ', href: '/login' },
  },

  invalid: {
    title: 'ลิงก์นี้ใช้ไม่ได้',
    body: 'ลิงก์อาจหมดอายุ ถูกใช้ไปแล้ว หรือคัดลอกมาไม่ครบ ลิงก์ตั้งรหัสผ่านใช้ได้ 30 นาทีและใช้ได้ครั้งเดียว',
    again: { label: 'ขอลิงก์ใหม่', href: '/forgot-password' },
  },

  errors: {
    tooShort: 'รหัสผ่านต้องยาวอย่างน้อย 8 ตัวอักษร',
    tooLong: 'รหัสผ่านยาวได้ไม่เกิน 256 ตัวอักษร',
    mismatch: 'รหัสผ่านทั้งสองช่องไม่ตรงกัน',
    unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  },
};

/** The link back to sign-in that both pages show under their form. */
export const backToSignIn = { label: 'กลับไปหน้าเข้าสู่ระบบ', href: '/login' };
