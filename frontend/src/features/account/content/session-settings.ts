/** The "อุปกรณ์ที่เข้าสู่ระบบอยู่" card on the staff account page. */
export const sessionSettings = {
  heading: 'อุปกรณ์ที่เข้าสู่ระบบอยู่',
  intro: 'ถ้าเห็นเครื่องที่ไม่รู้จัก ให้สั่งออกจากระบบแล้วเปลี่ยนรหัสผ่าน',
  empty: 'ไม่มีอุปกรณ์อื่นที่เข้าสู่ระบบอยู่',
  current: 'เครื่องนี้',
  unknownDevice: 'อุปกรณ์ที่ไม่รู้จัก',
  lastUsed: (when: string) => `ใช้ล่าสุด ${when}`,
  signedIn: (when: string) => `เข้าสู่ระบบ ${when}`,
  ip: (address: string) => `IP ${address}`,
  currentNote: 'ออกจากระบบเครื่องนี้ได้ที่ปุ่ม "ออกจากระบบ" ด้านบน',
  revoke: 'ออกจากระบบ',
  confirm: 'ยืนยันออกจากระบบเครื่องนี้',
  revoking: 'กำลังออกจากระบบ…',
  cancel: 'ยกเลิก',
};
