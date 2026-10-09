/** The list of API keys and revoking one (docs/contract-extensions.md § 22). */
export const apiKeyListText = {
  heading: 'API key ทั้งหมด',
  hint: 'แสดงเฉพาะรหัสอ้างอิงของ key ระบบไม่เก็บตัว key จึงแสดงซ้ำไม่ได้',
  empty: 'ยังไม่มี API key',

  columns: {
    name: 'ชื่อ',
    scopes: 'สิทธิ์',
    branches: 'สาขา',
    status: 'สถานะ',
    dates: 'วันที่',
    action: 'จัดการ',
  },

  allBranches: 'ทุกสาขา',
  created: (when: string) => `สร้าง ${when}`,
  expires: (when: string) => `หมดอายุ ${when}`,
  lastUsed: (when: string) => `ใช้ล่าสุด ${when}`,
  neverUsed: 'ยังไม่เคยใช้',
  revokedAt: (when: string) => `ยกเลิก ${when}`,

  status: {
    active: 'ใช้งานได้',
    expired: 'หมดอายุ',
    revoked: 'ยกเลิกแล้ว',
  },

  revoke: {
    button: 'ยกเลิก key',
    confirm: 'ระบบที่ใช้ key นี้จะถูกปฏิเสธตั้งแต่คำขอถัดไป และเปิดใช้อีกไม่ได้',
    confirmButton: 'ยืนยันยกเลิก key',
    pending: 'กำลังยกเลิก…',
    back: 'กลับ',
  },
};
