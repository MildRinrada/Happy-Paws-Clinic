/** "ปรับยอดหลังนับสต๊อก, ต้องระบุเหตุผล". */
export const adjustmentPage = {
  metaTitle: 'ปรับยอดสต๊อก · Happy Paws Clinic',
  title: 'ปรับยอดหลังนับสต๊อก',
  subtitle: 'กรอกจำนวนที่นับได้จริง ระบบคำนวณส่วนต่างเองและเก็บเหตุผลไว้ในประวัติ',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ปรับยอดสต๊อก (inventory:adjust)',
  noRead: 'ต้องมีสิทธิ์ดูสต๊อก (inventory:read) ด้วยจึงจะเลือกรายการที่จะนับได้',
  empty: 'ยังไม่มีรายการในคลังให้นับ',

  lot: 'ล็อตที่นับ',
  onlyLot: 'ล็อตเดียวที่มีอยู่ หรือของที่นับเจอโดยไม่มีล็อต',
  lotRequired: 'สาขานี้มีหลายล็อต เลือกล็อตที่นับ',
  booked: (count: number) => `ในระบบมี ${count}`,
  counted: 'นับได้',
  reason: 'เหตุผล',
  reasonPlaceholder: 'เช่น นับสิ้นเดือน พบเม็ดแตก 3 เม็ด',
  submit: 'บันทึกยอดที่นับ',
  saving: 'กำลังบันทึก…',
  badCount: 'จำนวนที่นับต้องเป็นจำนวนเต็มตั้งแต่ศูนย์ขึ้นไป',
  needReason: 'ต้องระบุเหตุผล',
  result: (difference: number, available: number, unit: string) =>
    `ปรับแล้ว ส่วนต่าง ${difference > 0 ? '+' : ''}${difference} ตอนนี้ใช้ได้ ${available} ${unit}`,
};
