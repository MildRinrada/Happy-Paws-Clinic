/** "สั่งซื้อจากซัพพลายเออร์, รับของบางส่วนได้". */
export const purchaseOrderPage = {
  metaTitle: 'สั่งซื้อยา · Happy Paws Clinic',
  title: 'สั่งซื้อจากซัพพลายเออร์',
  subtitle: 'ใบสั่งซื้อยังไม่เพิ่มสต๊อก เมื่อของมาให้บันทึกรับเข้าโดยเลือกใบสั่งซื้อนี้ รับทีละส่วนได้',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์สั่งซื้อ (inventory:purchase)',

  supplier: 'ซัพพลายเออร์',
  expectedOn: 'วันที่คาดว่าจะได้ของ',
  note: 'หมายเหตุ',
  lines: 'รายการสั่งซื้อ',
  quantity: 'จำนวน',
  unitCost: 'ต้นทุนต่อหน่วย (บาท)',
  addLine: 'เพิ่มรายการ',
  removeLine: 'เอาออก',
  lineHeading: (index: number) => `รายการที่ ${index}`,
  total: (amount: string) => `รวม ${amount}`,
  submit: 'ออกใบสั่งซื้อ',
  saving: 'กำลังบันทึก…',
  incomplete: 'กรอกรายการ จำนวน และต้นทุนให้ครบทุกแถว',

  result: {
    heading: (number: string) => `ออกใบสั่งซื้อ ${number} แล้ว`,
    line: (name: string, quantity: number, unit: string, cost: string) => `${name} ${quantity} ${unit} ${cost}`,
    next: 'เมื่อของมาถึง บันทึกรับเข้าที่หน้ารับเข้า จ่ายออก แล้วเลือกใบสั่งซื้อนี้',
    another: 'ออกใบสั่งซื้อใบใหม่',
  },
};
