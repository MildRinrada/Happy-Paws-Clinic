/** Everything the accept-invitation page says. Edit the wording here. */
export const invitationPage = {
  metaTitle: 'รับคำเชิญพนักงาน',
  brand: 'Happy Paws Clinic',
  heading: 'ตั้งค่าบัญชีพนักงาน',
  footnote: 'บัญชีพนักงานต้องยืนยันตัวตนสองขั้นตอนทุกครั้งที่เข้าสู่ระบบ',

  reading: 'กำลังอ่านลิงก์คำเชิญ',

  intro: {
    title: 'ยินดีต้อนรับ',
    body: 'คุณได้รับเชิญให้เข้าใช้ระบบของคลินิก ใช้เวลาประมาณ 2 นาที เตรียมโทรศัพท์ที่มีแอป authenticator ไว้ เช่น Google Authenticator หรือ Microsoft Authenticator',
    steps: ['สแกน QR code ด้วยแอป authenticator', 'ตั้งรหัสผ่าน แล้วกรอกรหัส 6 หลักจากแอป'],
    start: 'เริ่มตั้งค่า',
    starting: 'กำลังเตรียม',
  },

  setup: {
    stepLabel: 'ขั้นที่ 1 จาก 2',
    title: 'สแกน QR code ด้วยแอป authenticator',
    accountLabel: 'บัญชี',
    qrAlt: 'QR code สำหรับเพิ่มบัญชีในแอป authenticator',
    manualLabel: 'สแกนไม่ได้ ให้พิมพ์รหัสนี้ในแอปแทน',
    copy: 'คัดลอก',
    copied: 'คัดลอกแล้ว',
  },

  password: {
    stepLabel: 'ขั้นที่ 2 จาก 2',
    title: 'ตั้งรหัสผ่านและยืนยันแอป',
    passwordLabel: 'รหัสผ่านใหม่',
    passwordHint: 'อย่างน้อย 8 ตัวอักษร',
    confirmLabel: 'พิมพ์รหัสผ่านอีกครั้ง',
    codeLabel: 'รหัส 6 หลักจากแอป authenticator',
    codeHint: 'รหัสเปลี่ยนทุก 30 วินาที ใช้รหัสที่แสดงอยู่ตอนนี้',
    submit: 'สร้างบัญชี',
    submitting: 'กำลังสร้างบัญชี',
    tooShort: 'รหัสผ่านต้องยาวอย่างน้อย 8 ตัวอักษร',
    mismatch: 'รหัสผ่านทั้งสองช่องไม่ตรงกัน',
    badCode: 'รหัสยืนยันเป็นตัวเลข 6 หลักจากแอป',
  },

  accepted: {
    title: 'สร้างบัญชีเรียบร้อยแล้ว',
    body: 'เข้าสู่ระบบด้วยอีเมลนี้และรหัสผ่านที่เพิ่งตั้ง แล้วกรอกรหัสจากแอป authenticator',
    signIn: 'ไปหน้าเข้าสู่ระบบ',
  },

  invalid: {
    title: 'ลิงก์นี้ใช้ไม่ได้',
    body: 'ลิงก์คำเชิญอาจหมดอายุ (ใช้ได้ 7 วัน) ถูกใช้ไปแล้ว หรือคัดลอกมาไม่ครบ ขอให้ผู้ดูแลระบบของคลินิกส่งคำเชิญใหม่',
  },

  errors: {
    alreadyHasAccount: 'อีเมลนี้มีบัญชีในระบบแล้ว ให้เข้าสู่ระบบด้วยบัญชีเดิม',
    unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  },
};
