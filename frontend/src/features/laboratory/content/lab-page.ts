/** The lab worklist in the staff console, and the words a lab request shows wherever it appears. */
export const labPage = {
  metaTitle: 'งานแล็บ · Happy Paws Clinic',
  title: 'งานแล็บ',
  subtitle: (branch: string) => `คำขอตรวจแล็บของ${branch}`,
  noAccess: 'บัญชีนี้ดูงานแล็บไม่ได้ (lab:read)',
  noBranch: 'บัญชีนี้ยังไม่ได้ประจำสาขาใด',
  nextPage: 'หน้าถัดไป',

  tabs: {
    label: 'กรองตามสถานะ',
    pending: 'รอผล',
    resulted: 'ผลยังไม่มีคนอ่าน',
    reviewed: 'อ่านแล้ว',
    all: 'ทั้งหมด',
  },
  owner: {
    label: 'ผู้ส่งตรวจ',
    mine: 'ที่ฉันส่ง',
    everyone: 'ทุกคน',
  },
  empty: {
    pending: 'ไม่มีคำขอที่รอผล',
    resulted: 'ไม่มีผลที่รอหมออ่าน',
    reviewed: 'ยังไม่มีผลที่อ่านแล้ว',
    all: 'ยังไม่มีคำขอตรวจแล็บ',
    hint: 'สัตวแพทย์ส่งตรวจได้จากหน้าห้องตรวจ ในส่วนส่งตรวจแล็บ',
  },

  request: {
    status: { pending: 'รอผล', resulted: 'รออ่านผล', reviewed: 'อ่านแล้ว' },
    urgent: 'ด่วน',
    inHouse: 'แล็บในคลินิก',
    external: (name: string) => `แล็บภายนอก: ${name}`,
    requestedAt: (time: string) => `ส่งเมื่อ ${time}`,
    resultedAt: (time: string) => `ผลล่าสุด ${time}`,
    reviewedAt: (time: string) => `อ่านเมื่อ ${time}`,
    abnormal: (count: number) => `ผิดปกติ ${count} ค่า`,
    notes: 'ข้อมูลถึงแล็บ:',
    openCase: 'ดูเคส',
    openPet: 'ดูแฟ้มสัตว์',
  },

  results: {
    none: 'ยังไม่มีผล',
    analyte: 'รายการ',
    value: 'ผล',
    range: 'ช่วงอ้างอิง',
    flag: 'ธง',
    flags: { normal: 'ปกติ', low: 'ต่ำ', high: 'สูง', abnormal: 'ผิดปกติ' },
  },

  review: {
    submit: 'อ่านผลแล้ว',
    working: 'กำลังบันทึก…',
  },

  resultForm: {
    open: 'ลงผล',
    more: 'เพิ่มผล',
    cancel: 'ยกเลิก',
    analyte: 'รายการที่วัด',
    analytePlaceholder: 'เช่น ALT',
    value: 'ผล',
    valuePlaceholder: 'ตัวเลขหรือข้อความ',
    unit: 'หน่วย',
    low: 'ช่วงต่ำสุด',
    high: 'ช่วงสูงสุด',
    abnormal: 'ผิดปกติ',
    addRow: 'เพิ่มแถว',
    removeRow: 'ลบแถว',
    row: (index: number) => `ค่าที่ ${index}`,
    submit: 'บันทึกผล',
    working: 'กำลังบันทึก…',
    saved: 'บันทึกผลแล้ว',
    empty: 'กรอกอย่างน้อยหนึ่งค่า',
    noAnalyte: 'ใส่ชื่อรายการที่วัด',
    badRange: 'ช่วงอ้างอิงต้องเป็นตัวเลข',
    hint: 'ผลที่เป็นตัวเลขและมีช่วงอ้างอิง ระบบใส่ธงสูงหรือต่ำให้เอง ช่องผิดปกติใช้กับผลที่เป็นข้อความ บันทึกแล้วแก้ไม่ได้ ตรวจค่าก่อนกดบันทึก',
  },

  messages: {
    conflict: 'มีคนบันทึกคำขอนี้ไปก่อนแล้ว โหลดหน้าใหม่เพื่อดูข้อมูลล่าสุด',
    unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  },
};
