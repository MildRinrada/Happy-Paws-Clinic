/** The pharmacy counter in the staff console: "จ่ายยา". */
export const prescriptionsPage = {
  metaTitle: 'จ่ายยา · Happy Paws Clinic',
  title: 'จ่ายยา',
  subtitle: 'ค้นหาสัตว์ ดูประวัติยา แล้วจ่ายยาพร้อมพิมพ์ฉลาก',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูประวัติยา (rx:read)',
  petNotFound: 'ไม่พบสัตว์ตัวนี้',
  next: 'หน้าถัดไป',

  search: {
    label: 'ค้นหาสัตว์',
    placeholder: 'ชื่อสัตว์ เลข HN หรือไมโครชิป',
    submit: 'ค้นหา',
    clear: 'ล้าง',
    noScope: 'บัญชีนี้ค้นหาสัตว์ไม่ได้ (patient:read) เปิดจากลิงก์ในแฟ้มสัตว์แทน',
    resultsHeading: (q: string) => `ผลการค้นหา "${q}"`,
    none: 'ไม่พบสัตว์ที่ตรงกัน',
    open: 'ดูประวัติยา',
  },

  queue: {
    heading: 'รอจ่ายยา',
    hint: 'ใบสั่งยาที่ยังไม่จ่ายในสาขาของคุณ เปิดประวัติยาของสัตว์ก่อนจ่าย',
    empty: 'ไม่มีใบสั่งยาที่รอจ่าย',
    prescribedAt: (time: string) => `สั่งเมื่อ ${time}`,
  },

  pet: {
    back: 'ค้นหาสัตว์ตัวอื่น',
    profile: 'แฟ้มสัตว์',
    allergies: (list: string) => `แพ้: ${list}`,
    heading: 'ประวัติยา',
    hint: 'รายการที่ยังไม่จ่ายอยู่บนสุด',
    empty: 'สัตว์ตัวนี้ยังไม่เคยได้รับยา',
  },

  item: {
    line: (dose: string, frequency: string, days: number, quantity: number) =>
      `${dose} ${frequency} ${days} วัน (จ่าย ${quantity})`,
    prescribedAt: (time: string) => `สั่งเมื่อ ${time}`,
    courseEnds: (time: string) => `ยาหมด ${time}`,
    dispensedAt: (time: string) => `จ่ายเมื่อ ${time}`,
    acknowledged: (reason: string) => `สั่งทั้งที่มีประวัติแพ้: ${reason}`,
    consultation: 'เปิดเคส',
  },

  status: {
    waiting: 'รอจ่าย',
    dispensed: 'จ่ายแล้ว',
    active: 'กำลังใช้ยา',
  },

  duplicate: {
    badge: 'ยาซ้ำซ้อน',
    title: 'ช่วงกินยาทับกับใบสั่งยาตัวเดียวกัน',
    other: (drug: string, time: string) => `${drug} สั่งเมื่อ ${time}`,
    elsewhere: (count: number) => `อีก ${count} รายการที่ไม่อยู่ในหน้านี้`,
    note: 'ระบบเทียบจากคำแรกของชื่อยา ชื่อการค้าต่างกันจะไม่ถูกจับ',
  },

  dispense: {
    button: 'จ่ายยา',
    confirm: 'ยืนยันจ่ายยา',
    cancel: 'ยกเลิก',
    working: 'กำลังบันทึก…',
    warnDuplicate: 'ยานี้ซ้ำซ้อนกับใบสั่งอื่น ตรวจกับสัตวแพทย์ก่อนจ่าย',
    noScope: 'จ่ายยาได้เฉพาะบัญชีที่มีสิทธิ์ rx:dispense',
    unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
    done: 'บันทึกการจ่ายยาแล้ว',
  },

  stock: {
    deducted: (quantity: number, unit: string, name: string) => `ตัดสต๊อกแล้ว ${quantity} ${unit} (${name})`,
    notMatched:
      'ไม่พบยานี้ในคลัง ระบบจึงไม่ได้ตัดสต๊อกและไม่ได้คิดค่ายา ชื่อยาที่พิมพ์เองต้องตรงกับชื่อรายการในคลัง หรือให้สัตวแพทย์เลือกยาจากคลังตอนสั่ง',
    linked: 'ผูกกับสต๊อก',
    typed: 'ชื่อพิมพ์เอง จับคู่ด้วยชื่อตอนจ่าย',
    insufficient: (available: number, unit: string) =>
      `สต๊อกในระบบไม่พอ (ใช้ได้ ${available} ${unit}) จึงไม่ได้ตัดสต๊อก ควรตรวจนับและปรับยอด`,
    historyNotDeducted: 'ไม่ได้ตัดสต๊อก',
  },

  charge: {
    invoiced: (amount: string, number: string) => `คิดค่ายา ${amount} ลงบิล ${number} แล้ว`,
    onClose: (amount: string) => `ค่ายา ${amount} จะขึ้นบิลตอนปิดเคส`,
    separate: (amount: string, number: string) => `บิลของเคสรับเงินแล้ว จึงออกบิลค่ายาใบใหม่ ${number} ยอด ${amount}`,
    none: 'ไม่ได้คิดค่ายา',
  },

  label: {
    heading: 'ฉลากยา',
    print: 'พิมพ์ฉลาก',
    close: 'ซ่อนฉลาก',
    license: (no: string) => `ใบอนุญาต ${no}`,
    hn: 'HN',
    owner: 'เจ้าของ',
    howTo: (dose: string, frequency: string) => `ครั้งละ ${dose} ${frequency}`,
    course: (days: number, quantity: number) => `${days} วัน จำนวน ${quantity}`,
    prescribedBy: 'ผู้สั่ง',
    dispensedOn: 'วันที่จ่าย',
  },
};
