/** The "ยืนยันตัวตนสองขั้นตอน" card on the staff account page. */
export const twoFactorSettings = {
  heading: 'ยืนยันตัวตนสองขั้นตอน',

  status: {
    on: 'เปิดใช้อยู่',
    off: 'ยังไม่ได้ตั้ง',
    methods: { totp: 'แอป authenticator', sms: 'SMS', none: 'ไม่มี' } as Record<string, string>,
    recoveryLeft: (count: number) => `เหลือรหัสสำรอง ${count} รหัส`,
    recoveryLow: 'รหัสสำรองใกล้หมด ตั้งค่าใหม่เพื่อรับชุดใหม่',
  },

  intro: {
    enabled:
      'ใช้เมื่อเปลี่ยนโทรศัพท์ ลบแอปไปแล้ว หรืออยากได้รหัสสำรองชุดใหม่ แอปเดิมยังใช้ได้จนกว่าจะยืนยันแอปใหม่สำเร็จ',
    disabled: 'ทุกครั้งที่เข้าสู่ระบบ ระบบจะถามรหัส 6 หลักจากแอปในโทรศัพท์ เพิ่มเติมจากรหัสผ่าน',
  },
  start: 'ตั้งค่าแอป authenticator',
  restart: 'ตั้งค่าใหม่',

  password: {
    label: 'รหัสผ่านปัจจุบัน',
    hint: 'ยืนยันว่าเป็นคุณก่อนสร้างรหัสลับใหม่',
    submit: 'ถัดไป',
    submitting: 'กำลังตรวจ…',
  },

  scan: {
    title: 'สแกน QR code ด้วยแอป authenticator',
    body: 'เปิดแอป เช่น Google Authenticator หรือ Microsoft Authenticator แล้วเพิ่มบัญชีด้วยการสแกน',
    qrAlt: 'QR code สำหรับเพิ่มบัญชีในแอป authenticator',
    manualLabel: 'สแกนไม่ได้ ให้พิมพ์รหัสลับนี้ในแอปแทน',
  },

  code: {
    label: 'รหัส 6 หลักจากแอป',
    hint: 'รหัสเปลี่ยนทุก 30 วินาที ใช้รหัสที่แสดงอยู่ตอนนี้ พิมพ์เว้นวรรคได้',
    invalid: 'กรอกรหัสตัวเลข 6 หลักจากแอป',
    submit: 'ยืนยันและเปิดใช้',
    submitting: 'กำลังยืนยัน…',
  },

  recovery: {
    title: 'รหัสสำรอง',
    warning:
      'เก็บรหัสชุดนี้ไว้ในที่ปลอดภัยตอนนี้ ระบบจะไม่แสดงอีก ใช้แทนรหัสจากแอปได้รหัสละครั้งเดียวเมื่อโทรศัพท์หาย รหัสสำรองชุดเดิมใช้ไม่ได้แล้ว',
    listLabel: 'รหัสสำรอง 8 รหัส',
    copy: 'คัดลอกทั้งหมด',
    done: 'เก็บรหัสแล้ว',
  },

  enabled: 'เปิดใช้แล้ว การเข้าสู่ระบบครั้งถัดไปจะถามรหัสจากแอปนี้',
  cancel: 'ยกเลิก',
  copy: 'คัดลอก',
  copied: 'คัดลอกแล้ว',

  errors: {
    /** `409 state_conflict`: the first step was never taken, or another tab finished it. */
    notStarted: 'ขั้นตอนนี้หมดอายุแล้ว เริ่มตั้งค่าใหม่อีกครั้ง',
    /** `403`: replacing a working factor needs a session that passed it. */
    needsSecondFactor:
      'เปลี่ยนแอปได้เฉพาะเมื่อเข้าสู่ระบบด้วยรหัสจากแอปเดิม ออกจากระบบแล้วเข้าใหม่ด้วยรหัสจากแอปเดิม แล้วลองอีกครั้ง',
  },
};
