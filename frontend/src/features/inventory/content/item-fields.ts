/** The words of the item picker and the "new item" fields, shared by receipts and purchase orders. */
export const itemFields = {
  item: 'รายการ',
  choose: 'เลือกรายการ',
  newItem: 'รายการใหม่ (ยังไม่มีในคลัง)',
  option: (name: string, available: number, unit: string) => `${name} (ใช้ได้ ${available} ${unit})`,
  noRead: 'บัญชีนี้ดูรายการในคลังไม่ได้ (inventory:read) จึงเลือกได้เฉพาะรายการใหม่',
  name: 'ชื่อรายการ',
  nameHint: 'ระบบผูกใบสั่งยากับรายการด้วยชื่อนี้ ต้องตรงกับชื่อยาที่หมอเขียน (ไม่สนตัวพิมพ์เล็กใหญ่และช่องว่างซ้ำ)',
  kind: 'ประเภท',
  unit: 'หน่วย',
  unitPlaceholder: 'เม็ด',
  price: 'ราคาขายต่อหน่วย (บาท)',
  priceHint: 'คิดเงินลูกค้าตอนจ่ายยา ใส่ 0 ถ้าไม่คิดเงิน',
  reorder: 'จุดสั่งซื้อ',
  reorderHint: 'เหลือเท่านี้หรือน้อยกว่าจะขึ้นว่าใกล้หมด',
  badAmount: 'กรอกเป็นตัวเลข เช่น 5 หรือ 5.50',
};
