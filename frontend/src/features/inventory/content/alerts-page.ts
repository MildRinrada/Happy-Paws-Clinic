/** "ใกล้หมด ใกล้หมดอายุ ล็อตที่ต้องเรียกคืน". */
export const alertsPage = {
  metaTitle: 'แจ้งเตือนสต๊อก · Happy Paws Clinic',
  title: 'แจ้งเตือนสต๊อก',
  subtitle: 'เรียงจากเรื่องที่ต้องทำก่อน ล็อตหมดอายุ ยาที่หมด ยาใกล้หมดอายุ แล้วยาใกล้หมด',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูสต๊อก (inventory:read)',
  empty: 'ไม่มีเรื่องที่ต้องดูตอนนี้',
  window: 'ดูวันหมดอายุล่วงหน้า',
  windowOption: (days: number) => `${days} วัน`,
  apply: 'แสดง',

  available: (count: number, unit: string) => `ใช้ได้ ${count} ${unit}`,
  reorder: (level: number) => `จุดสั่งซื้อ ${level}`,
  onOrder: (count: number) => `สั่งไว้แล้ว ${count}`,
  lot: (number: string | undefined, quantity: number) => `ล็อต ${number ?? 'ไม่มีเลข'} เหลือ ${quantity}`,
  expiresIn: (days: number) => (days < 0 ? `หมดอายุมาแล้ว ${-days} วัน` : days === 0 ? 'หมดอายุวันนี้' : `หมดอายุในอีก ${days} วัน`),
  recallNote: 'ล็อตที่หมดอายุยังอยู่ในยอด ใช้หน้าจ่ายออกเลือกล็อตนั้นเพื่อนำออกจากคลัง',
};
