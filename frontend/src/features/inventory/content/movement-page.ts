/** "รับเข้า จ่ายออก โอนสาขา, อ้างอิงเอกสารต้นทาง". */
export const movementPage = {
  metaTitle: 'รับเข้า จ่ายออก · Happy Paws Clinic',
  title: 'รับเข้า จ่ายออก โอนสาขา',
  subtitle: 'ทุกครั้งที่ของเข้าหรือออกจากคลัง บันทึกเป็นรายการในสมุดสต๊อก แก้ย้อนหลังไม่ได้',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์บันทึกการรับเข้าหรือจ่ายออก (inventory:write)',

  types: {
    receive: 'รับเข้า',
    issue: 'จ่ายออก',
    transfer: 'โอนสาขา',
  },
  type: 'ทำอะไร',
  quantity: 'จำนวน',
  lotNumber: 'เลขล็อต',
  expiresOn: 'วันหมดอายุ',
  unitCost: 'ต้นทุนต่อหน่วย (บาท)',
  unitCostHint: 'ไม่กรอกก็ได้ ถ้ารับตามใบสั่งซื้อจะใช้ต้นทุนในใบสั่งซื้อ',
  order: 'รับตามใบสั่งซื้อ',
  noOrder: 'ไม่ผ่านใบสั่งซื้อ',
  orderOption: (number: string, outstanding: number) => `${number} (ค้างรับ ${outstanding})`,
  lot: 'ล็อต',
  autoLot: 'อัตโนมัติ (หมดอายุก่อน ออกก่อน)',
  lotOption: (number: string | undefined, quantity: number, expired: boolean) =>
    `${number ?? 'ไม่มีเลขล็อต'} เหลือ ${quantity}${expired ? ' (หมดอายุ)' : ''}`,
  toBranch: 'โอนไปสาขา',
  chooseBranch: 'เลือกสาขา',
  reference: 'เลขเอกสารอ้างอิง',
  referencePlaceholder: 'ใบส่งของ ใบโอน',
  note: 'หมายเหตุ',
  submit: 'บันทึก',
  saving: 'กำลังบันทึก…',
  badQuantity: 'จำนวนต้องเป็นจำนวนเต็มมากกว่าศูนย์',

  result: {
    heading: 'บันทึกแล้ว',
    row: (label: string, quantity: number, lot: string | undefined, balance: number) =>
      `${label} ${quantity > 0 ? '+' : ''}${quantity} ล็อต ${lot ?? 'ไม่มีเลข'} คงเหลือในล็อต ${balance}`,
    available: (name: string, count: number, unit: string) => `${name} ใช้ได้ ${count} ${unit}`,
    order: (number: string, status: string) => `ใบสั่งซื้อ ${number}: ${status}`,
  },
};
