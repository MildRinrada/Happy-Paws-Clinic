/** `/billing/new`: a bill raised by hand at the counter. */
export const createInvoicePage = {
  metaTitle: 'สร้างบิล · Happy Paws Clinic',
  title: 'สร้างบิล',
  subtitle: 'สำหรับขายหน้าร้าน ค่าฝากเลี้ยง หรือค่าบริการที่ไม่ได้มาจากการปิดเคส บิลค่ารักษาออกเองตอนสัตวแพทย์ปิดเคส',
  openButton: 'สร้างบิล',
  back: '← กลับไปรายการบิล',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์สร้างบิล (invoice:write)',
  noBranch: 'ยังไม่มีสาขาที่บัญชีนี้เข้าถึงได้',

  customer: {
    heading: 'ลูกค้า',
    label: 'ค้นหาลูกค้า',
    placeholder: 'ชื่อ เบอร์โทร HN หรือชื่อสัตว์',
    submit: 'ค้นหา',
    searching: 'กำลังค้นหา…',
    empty: 'ไม่พบลูกค้าที่ตรงกับคำค้น',
    pick: 'เลือก',
    change: 'เปลี่ยนลูกค้า',
    noSearch: 'บัญชีนี้ค้นหาลูกค้าไม่ได้ (ต้องมีสิทธิ์ customer:read) กรอกรหัสลูกค้าแทน',
    idLabel: 'รหัสลูกค้า',
    idPlaceholder: 'cus_…',
  },

  pet: {
    label: 'สัตว์เลี้ยง (ไม่บังคับ)',
    none: 'ไม่ระบุ',
  },

  lines: {
    heading: 'รายการ',
    lineHeading: (index: number) => `รายการที่ ${index}`,
    pick: 'เลือกจากรายการ',
    pickNone: 'พิมพ์เอง',
    services: 'บริการ',
    items: 'ยาและเวชภัณฑ์',
    description: 'รายละเอียด',
    kind: 'ประเภท',
    quantity: 'จำนวน',
    unitPrice: 'ราคาต่อหน่วย (บาท)',
    add: 'เพิ่มรายการ',
    remove: 'เอาออก',
    itemOption: (name: string, price: string, available: number, unit: string) => `${name} ${price} (เหลือ ${available} ${unit})`,
    serviceOption: (name: string, price: string) => `${name} ${price}`,
    medicineNote: 'ค่ายาที่ใส่ตรงนี้ไม่ตัดสต๊อก ถ้าหยิบของออกจากคลังจริง ให้บันทึกจ่ายออกที่หน้าสต๊อกด้วย',
    catalogueNote: 'ราคาที่ดึงมาเป็นราคาตั้งต้น แก้ได้ก่อนบันทึก บริการที่ราคาต่างกันตามชนิดหรือน้ำหนักสัตว์ ให้แก้เป็นราคาที่ใช้จริง',
  },

  note: 'หมายเหตุ (ไม่บังคับ)',
  totals: {
    subtotal: 'ยอดรวมโดยประมาณ',
    hint: 'ระบบคำนวณยอดจริงตอนบันทึก ส่วนลดให้ได้หลังสร้างบิล',
  },
  submit: 'สร้างบิล',
  saving: 'กำลังสร้างบิล…',
  incomplete: 'เลือกลูกค้า และกรอกรายละเอียด จำนวน และราคาให้ครบทุกรายการ',
};
