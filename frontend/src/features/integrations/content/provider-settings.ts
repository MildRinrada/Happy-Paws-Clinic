/** Words both provider forms share. */
export const providerShared = {
  notConnected: 'ระบบยังไม่มีตัวเชื่อมต่อกับผู้ให้บริการรายใด หน้านี้เก็บค่าตั้งและตรวจรูปแบบเท่านั้น เปิดใช้แล้วยังไม่มีผลกับการทำงาน',
  storedPlaceholder: 'ตั้งไว้แล้ว เว้นว่างเพื่อใช้ค่าเดิม',
  emptyPlaceholder: 'ยังไม่ได้ตั้ง',
  secretHint: 'ระบบไม่แสดงค่าที่บันทึกแล้ว และเก็บแบบเข้ารหัส',
  clear: 'ลบค่าที่บันทึกไว้',
  providerHint: 'ตัวอักษรอังกฤษพิมพ์เล็ก ตัวเลข หรือ _ เช่น opn หรือ 2c2p',
  set: 'ตั้งไว้แล้ว',
  unset: 'ยังไม่ได้ตั้ง',
};

/** The payment provider page. */
export const paymentSettingsPage = {
  metaTitle: 'ผู้ให้บริการชำระเงิน · Happy Paws Clinic',
  title: 'ผู้ให้บริการชำระเงิน',
  subtitle: 'บัญชีรับชำระเงินออนไลน์ของคลินิก สำหรับการชำระผ่านเว็บในอนาคต',
  heading: 'ค่าตั้ง',
  provider: 'ผู้ให้บริการ',
  mode: 'โหมด',
  modes: { test: 'ทดสอบ (test)', live: 'ใช้งานจริง (live)' } as Record<string, string>,
  publicKey: 'Public key หรือ merchant ID',
  secretKey: 'Secret key',
  webhookSecret: 'Webhook secret ของผู้ให้บริการ',
  enabled: 'เปิดใช้',
  enabledHint: 'เปิดได้เมื่อระบุผู้ให้บริการและ secret key แล้ว',
};

/** The outside lab page. */
export const labSettingsPage = {
  metaTitle: 'แล็บภายนอก · Happy Paws Clinic',
  title: 'แล็บภายนอก',
  subtitle: 'แล็บที่คลินิกส่งตัวอย่างไปตรวจ สำหรับการรับผลอัตโนมัติในอนาคต ตอนนี้ผลแล็บยังบันทึกด้วยมือ',
  heading: 'ค่าตั้ง',
  provider: 'แล็บ',
  accountId: 'รหัสบัญชีของคลินิกที่แล็บ',
  baseUrl: 'URL ของ API แล็บ',
  baseUrlHint: 'ต้องขึ้นต้นด้วย https:// และไม่ใช่ที่อยู่ภายในเครือข่าย',
  apiKey: 'API key ของแล็บ',
  enabled: 'เปิดใช้',
  enabledHint: 'เปิดได้เมื่อระบุแล็บแล้ว',
};
