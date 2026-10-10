/** The rules page: list, stats, create form, the switch, edit and delete, and the preview. */
export const automationsPage = {
  metaTitle: 'เตือนอัตโนมัติ · Happy Paws Clinic',
  title: 'เตือนอัตโนมัติ',
  subtitle: 'กติกาที่ระบบใช้ส่งข้อความเตือนเอง เช่น ครบกำหนดวัคซีนหรือก่อนถึงนัด ทุกข้อความผ่านการตรวจความยินยอมเหมือนการส่งปกติ',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูกติกาเตือนอัตโนมัติ',
  empty: 'ยังไม่มีกติกา',
  startHint: 'กติกาใหม่และกติกาเริ่มต้นปิดไว้ก่อน ลองกดดูตัวอย่างผู้รับ แล้วค่อยเปิดสวิตช์ของกติกานั้น',

  enabled: 'เปิดใช้',
  disabled: 'ปิดอยู่',
  starter: 'กติกาเริ่มต้น',
  clinicWide: 'ทั้งคลินิก',

  stats: {
    sent: 'ส่งแล้ว',
    skipped: 'ข้าม',
    failed: 'ไม่สำเร็จ',
    recent: '30 วันล่าสุด',
    lastFired: 'ครั้งล่าสุด',
    never: 'ยังไม่เคยส่ง',
  },

  timing: {
    hoursBefore: (hours: number) => `${hours} ชั่วโมงก่อนถึงเวลา`,
    daysBefore: (days: number, time: string) => `${days} วันก่อน เวลา ${time} น.`,
    sameDay: (time: string) => `วันนั้น เวลา ${time} น.`,
    daysAfter: (days: number, time: string) => `${days} วันหลังครบกำหนด เวลา ${time} น.`,
    daysAfterDueBack: (days: number, time: string) => `${days} วันหลังวันที่ควรกลับมา เวลา ${time} น.`,
    daysAfterVisit: (days: number, time: string) => `${days} วันหลังวันที่มาใช้บริการ เวลา ${time} น.`,
  },
  conditions: {
    species: (value: string) => `เฉพาะ${value}`,
    vaccine: (value: string) => `เฉพาะวัคซีน ${value}`,
    unconfirmed: 'เฉพาะนัดที่ยังไม่ยืนยัน',
  },

  manage: {
    switchLabel: (name: string) => `เปิดใช้กติกา ${name}`,
    confirmOn: 'เปิดแล้วระบบจะเริ่มส่งข้อความถึงลูกค้าจริงในรอบถัดไป',
    confirmOnButton: 'ยืนยันเปิดใช้',
    switching: 'กำลังบันทึก…',
    edit: 'แก้ไข',
    remove: 'ลบ',
    confirmRemove: 'ลบกติกานี้ ข้อความที่ส่งไปแล้วยังอยู่ในประวัติการส่ง',
    confirmRemoveButton: 'ยืนยันลบ',
    removing: 'กำลังลบ…',
    cancel: 'ยกเลิก',
    clinicWideLocked: 'กติกาทั้งคลินิก แก้ เปิดปิด หรือลบได้เฉพาะผู้ที่ไม่ถูกจำกัดสาขา',
  },

  edit: {
    watches: (trigger: string) => `เรื่องที่เฝ้าดู: ${trigger} เปลี่ยนไม่ได้ ถ้าต้องการเตือนเรื่องอื่นให้สร้างกติกาใหม่`,
    noRepeat: 'ลูกค้าที่ได้ข้อความจากกติกานี้ไปแล้วจะไม่ได้ซ้ำ แม้จะเปลี่ยนเวลาส่ง',
    save: 'บันทึก',
    saving: 'กำลังบันทึก…',
    saved: 'บันทึกแล้ว',
    cancel: 'ยกเลิก',
  },

  simulate: {
    button: 'ดูตัวอย่างผู้รับ',
    running: 'กำลังคำนวณ…',
    days: 'ดูล่วงหน้า',
    dayChoices: [1, 3, 7, 14, 30],
    dayLabel: (days: number) => `${days} วัน`,
    summary: (send: number, skip: number, sent: number) => `จะส่ง ${send} คน ข้าม ${skip} คน ส่งไปแล้ว ${sent} คน`,
    dueNow: (count: number) => `ถ้าเปิดใช้ตอนนี้ จะส่งทันที ${count} ข้อความ`,
    truncated: 'แสดงเฉพาะ 500 รายการแรก',
    empty: 'ในช่วงนี้ยังไม่มีใครเข้าเงื่อนไข',
    close: 'ซ่อน',
  },

  create: {
    heading: 'สร้างกติกาใหม่',
    name: 'ชื่อกติกา',
    namePlaceholder: 'เช่น เตือนวัคซีนล่วงหน้า 14 วัน',
    trigger: 'เมื่อไร',
    timingMode: 'นับเวลาแบบ',
    modeDays: 'นับเป็นวัน',
    modeHours: 'นับเป็นชั่วโมง',
    offsetDays: 'กี่วันก่อน',
    offsetDaysHint: 'ใส่ 0 คือวันเดียวกัน วัคซีนใส่ค่าติดลบได้ คือหลังครบกำหนด',
    offsetDaysLapsed: 'กี่วันก่อน (ใส่ค่าติดลบ)',
    offsetDaysLapsedHint: 'ใส่ -1 ถึง -90 เช่น -7 คือส่ง 7 วันหลังวันที่ลูกค้าควรกลับมาตามรอบของตัวเอง',
    lapsedDefaultDays: '-7',
    offsetDaysReview: 'กี่วันก่อน (ใส่ค่าติดลบ)',
    offsetDaysReviewHint: 'ใส่ -1 ถึง -30 เช่น -2 คือส่ง 2 วันหลังวันที่มาใช้บริการ',
    reviewDefaultDays: '-2',
    offsetHours: 'กี่ชั่วโมงก่อน',
    sendTime: 'ส่งเวลา',
    species: 'เฉพาะชนิดสัตว์',
    speciesHint: 'เว้นว่างได้ เช่น สุนัข',
    vaccineName: 'เฉพาะวัคซีน',
    vaccineHint: 'เว้นว่างได้ เช่น Rabies',
    unconfirmedOnly: 'เฉพาะนัดที่ลูกค้ายังไม่ยืนยัน',
    channel: 'ส่งทาง',
    enabled: 'เปิดใช้ทันที',
    enabledHint: 'ถ้าไม่ติ๊ก กติกาจะถูกสร้างแบบปิดไว้ ใช้ดูตัวอย่างผู้รับได้ แต่ยังไม่ส่งจริง',
    branchNote: (branch: string) => `กติกานี้ใช้กับ${branch}`,
    submit: 'สร้างกติกา',
    submitting: 'กำลังสร้าง…',
    created: 'สร้างแล้ว',
  },

  /** The review invitation trigger (docs/contract-extensions.md § 36). */
  review: {
    consentTitle: 'ใช้ความยินยอมรับข่าวสาร ไม่ใช่ความยินยอมเรื่องการรักษา',
    consentBody:
      'การขอรีวิวเป็นข้อความการตลาด ลูกค้าที่ยังไม่ได้ยินยอมรับข่าวสาร (ค่าตั้งต้นคือไม่ยินยอม) จะถูกข้ามและบันทึกว่าไม่มีความยินยอม แม้จะรับข้อความเตือนวัคซีนหรือนัดได้ตามปกติ',
    ruleBody:
      'ส่งถึงลูกค้าที่มาใช้บริการแล้ว คือมีการตรวจที่ปิดเคสแล้ว หรือมีบิลที่ไม่ได้ยกเลิกของสัตว์เลี้ยง หนึ่งครั้งต่อการมาหนึ่งครั้ง และไม่ส่งถ้ารีวิวไปแล้ว ลูกค้าที่มีบัญชีออนไลน์ได้ลิงก์หน้ารีวิวในระบบ คนอื่นได้ลิงก์ที่ใช้ได้ครั้งเดียวภายใน 30 วัน',
  },

  /** The "คิดถึงน้อง" trigger (docs/contract-extensions.md § 33). */
  lapsed: {
    consentTitle: 'ใช้ความยินยอมรับข่าวสาร ไม่ใช่ความยินยอมเรื่องการรักษา',
    consentBody:
      'ข้อความชวนกลับมาเป็นข้อความการตลาด ลูกค้าที่ยังไม่ได้ยินยอมรับข่าวสาร (ค่าตั้งต้นคือไม่ยินยอม) จะถูกข้ามและบันทึกว่าไม่มีความยินยอม แม้จะรับข้อความเตือนวัคซีนหรือนัดได้ตามปกติ',
    ruleBody:
      'ลูกค้าที่มาอย่างน้อย 3 ครั้ง และหายไปนานกว่ารอบปกติของตัวเอง (มากกว่า 1.5 เท่าของระยะห่างกลางระหว่างครั้ง หรือระยะห่างกลางบวก 30 วัน แล้วแต่ค่าไหนมากกว่า) ยังมีสัตว์ที่มีชีวิต และยังไม่มีนัดล่วงหน้า แต่ละคนได้ข้อความหนึ่งครั้งต่อการหายไปหนึ่งรอบ',
  },
};
