/** Words for marking a no-show and what it does to the customer's record. */
export const noShowText = {
  hint: 'บันทึกไม่มาตามนัดได้หลังเลยเวลานัดแล้ว',
  confirmTitle: 'บันทึกว่าไม่มาตามนัด',
  confirmBody: 'นัดนี้จะปิดเป็นไม่มาตามนัด และนับเป็นหนึ่งครั้งในสถิติของลูกค้าตามนโยบายยกเลิก ย้อนกลับไม่ได้',
  submit: 'ยืนยันไม่มาตามนัด',
  back: 'ไม่บันทึก',
  done: 'บันทึกว่าไม่มาตามนัดแล้ว',
  record: (noShows: number, lateCancellations: number) =>
    `ลูกค้าคนนี้ไม่มาตามนัดทั้งหมด ${noShows} ครั้ง ยกเลิกกระชั้น ${lateCancellations} ครั้ง`,
  strikes: (strikes: number, windowDays: number, allowance: number) =>
    `ใน ${windowDays} วันล่าสุดนับได้ ${strikes} ครั้ง นโยบายยอมให้ ${allowance} ครั้ง`,
  fee: (amount: string) => `ครั้งนี้มีค่าปรับตามนโยบาย ${amount}`,
  feeNote: 'ระบบไม่ได้ออกบิลให้ ถ้าจะเก็บ ให้ออกใบแจ้งหนี้ที่เคาน์เตอร์ตอนลูกค้ามา',
  noFee: 'ครั้งนี้ไม่มีค่าปรับ ยังอยู่ในจำนวนครั้งที่ยอมให้ หรือนโยบายไม่ได้ตั้งค่าปรับไว้',
};

/** What each conflict kind means, for the line beside the backend's own sentence. */
export const conflictKindLabel: Record<string, string> = {
  clinic_closed: 'คลินิกปิด',
  staff_off_shift: 'หมอไม่มีเวร',
  staff_on_time_off: 'หมอลา',
  staff_double_booked: 'หมอมีนัดซ้อน',
  pet_double_booked: 'สัตว์มีนัดซ้อน',
};

/** Checking a time before booking it. */
export const conflictCheckText = {
  check: 'ตรวจนัดชน',
  checkAll: 'ตรวจนัดชนทุกครั้ง',
  checking: 'กำลังตรวจ…',
  clear: 'ไม่พบปัญหา',
  stale: 'ข้อมูลในฟอร์มเปลี่ยนแล้ว กดตรวจอีกครั้ง',
  noVet: 'ยังไม่ได้เลือกหมอ จึงตรวจได้แค่วันหยุดและนัดซ้อนของสัตว์ เลือกหมอเพื่อตรวจเวรและนัดของหมอด้วย',
  reportOnly: 'การตรวจแค่รายงาน ไม่ได้กันเวลาไว้ ระบบตรวจอีกครั้งตอนจองจริง และไม่ตรวจห้องเพราะนัดไม่ได้จองห้อง',
  found: (count: number) => `พบ ${count} เรื่อง`,
};

/** The "book a course" part of the new-appointment form. */
export const repeatText = {
  toggle: 'จองเป็นคอร์ส (นัดซ้ำหลายครั้ง)',
  toggleHint: 'เช่น เคมีบำบัดทุก 3 สัปดาห์ 6 ครั้ง เวลาเดิมทุกครั้ง หมอคนเดียวตลอดคอร์ส',
  legend: 'นัดซ้ำ',
  frequency: 'ซ้ำทุก',
  daily: 'วัน',
  weekly: 'สัปดาห์',
  end: 'จบเมื่อ',
  byCount: 'ครบจำนวนครั้ง',
  byUntil: 'ถึงวันที่',
  count: 'จำนวนครั้ง',
  until: 'วันสุดท้าย',
  weeklyNote: 'ซ้ำในวันเดียวกับครั้งแรกของสัปดาห์',
  previewTitle: (count: number) => `วันที่จะจอง ${count} ครั้ง`,
  previewEmpty: 'เลือกวันและเวลาครั้งแรกก่อน แล้ววันที่ทั้งหมดจะแสดงตรงนี้',
  visit: (index: number) => `ครั้งที่ ${index}`,
  tooFew: 'คอร์สต้องมีอย่างน้อย 2 ครั้ง',
  tooMany: 'คอร์สมีได้ไม่เกิน 52 ครั้ง',
  tooFar: 'ทุกครั้งต้องอยู่ภายใน 366 วันนับจากครั้งแรก',
  badInterval: 'ใส่ช่วงห่างเป็นจำนวนเต็มตั้งแต่ 1',
  badUntil: 'เลือกวันสุดท้ายที่ไม่ก่อนครั้งแรก',
  allOrNothing: 'ระบบจองทั้งชุดหรือไม่จองเลย ถ้ามีครั้งใดจองไม่ได้ จะไม่มีนัดไหนถูกจอง และบอกเหตุผลทีละวัน',
  oneVet: 'ถ้าไม่เลือกหมอ ระบบเลือกหมอที่ว่างในครั้งแรก แล้วใช้หมอคนนั้นทุกครั้ง',
  refused: 'จองไม่ได้',
  suggested: 'วันนั้นยังว่าง',
  submit: (count: number) => `จองคอร์ส ${count} ครั้ง`,
};

/** The waiting list page. */
export const waitingListText = {
  metaTitle: 'คิวรอคิวว่าง · Happy Paws Clinic',
  title: 'คิวรอคิวว่าง',
  subtitle: 'ใส่สัตว์เลี้ยงไว้ในคิวรอ เมื่อมีคนยกเลิกนัดที่ตรงกัน ระบบส่งข้อความหาเจ้าของ',
  back: 'กลับไปปฏิทิน',
  link: 'คิวรอคิวว่าง',
  noAccess: 'บัญชีนี้ใส่คิวรอไม่ได้ (appointment:write)',
  how: [
    'เมื่อมีคนยกเลิกนัดที่ตรงบริการ วัน สาขา และหมอที่ระบุ ระบบแจ้งคนที่รอไม่เกินสามคน ตามลำดับที่เข้าคิว',
    'ระบบไม่ได้จองให้ ใครจองก่อนได้ก่อนผ่านการจองปกติ',
    'ข้อความไปหาเจ้าของหลักของสัตว์ ตามความยินยอมที่เจ้าของให้ไว้',
    'สัตว์หนึ่งตัวรอบริการเดียวได้ครั้งเดียว',
    'รายการหมดอายุเองหลังวันสุดท้ายที่รอ ถ้าเจ้าของไม่ต้องการรอแล้ว กดถอนออกจากรายการด้านล่าง',
  ],
  addHeading: 'ใส่สัตว์เลี้ยงในคิวรอ',
  service: 'บริการ',
  noService: 'ยังไม่มีบริการที่สาขานี้',
  earliest: 'วันแรกที่สะดวก',
  latest: 'วันสุดท้ายที่รอ',
  windowHint: 'รอได้ไกลสุด 90 วันจากวันนี้',
  vet: 'สัตวแพทย์',
  anyVet: 'หมอคนไหนก็ได้',
  thisBranch: 'เฉพาะสาขานี้',
  thisBranchHint: 'ไม่เลือก คือแจ้งเมื่อมีที่ว่างทุกสาขา',
  note: 'หมายเหตุ (ไม่บังคับ)',
  notePlaceholder: 'เช่น สะดวกช่วงเช้าเท่านั้น',
  missing: 'เลือกสัตว์เลี้ยงและบริการก่อน',
  submit: 'ใส่คิวรอ',
  submitting: 'กำลังเพิ่ม…',
  added: 'เพิ่มในคิวรอแล้ว',
  addedWindow: (from: string, to: string) => `รอตั้งแต่ ${from} ถึง ${to}`,
  addAnother: 'เพิ่มอีกตัว',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  list: {
    heading: 'รายการคิวรอ',
    note: 'แสดงรายการของสาขานี้ และรายการที่รอทุกสาขา เรียงตามลำดับที่เข้าคิว ซึ่งเป็นลำดับที่ระบบแจ้ง',
    tabs: { waiting: 'กำลังรอ', lapsed: 'หมดอายุแล้ว' },
    empty: { waiting: 'ยังไม่มีใครรอคิวว่าง', lapsed: 'ไม่มีรายการที่หมดอายุแล้ว' },
    window: (from: string, to: string) => `รอ ${from} ถึง ${to}`,
    anyBranch: 'ทุกสาขา',
    thisBranch: 'เฉพาะสาขานี้',
    vet: (name: string) => `หมอ ${name}`,
    notified: (count: number) => (count === 0 ? 'ยังไม่เคยได้รับแจ้ง' : `แจ้งแล้ว ${count} ครั้ง`),
    owner: 'หน้าลูกค้า',
    next: 'หน้าถัดไป',
    first: 'กลับหน้าแรก',
  },
  remove: {
    action: 'ถอนออก',
    confirm: 'ยืนยันถอนออก',
    keep: 'ไม่ถอน',
    working: 'กำลังถอน…',
    question: (pet: string) => `ถอน ${pet} ออกจากคิวรอ ระบบจะไม่แจ้งเรื่องคิวว่างของรายการนี้อีก`,
    gone: 'รายการนี้ถูกถอนออกไปแล้ว',
  },
};

/** "ตรวจนัดชนทั้งวัน" on the calendar: every visit already booked on the day in view. */
export const dayConflictText = {
  check: 'ตรวจนัดชนทั้งวัน',
  checking: 'กำลังตรวจ…',
  hint: 'ตรวจนัดที่จองไว้แล้วของวันนี้ที่สาขานี้ กับวันหยุด เวรหมอ วันลา และนัดซ้อน เช่น หลังบันทึกวันลาของหมอ',
  clear: 'ไม่พบนัดที่มีปัญหาในวันนี้',
  found: (count: number) => `พบ ${count} เรื่อง`,
  appointment: 'นัด',
  window: (from: string, to: string) => `${from} ถึง ${to}`,
  unknownAppointment: 'นัดที่ไม่อยู่ในปฏิทินที่เปิดอยู่',
  reportOnly: 'การตรวจแค่รายงาน ไม่ได้ย้ายหรือยกเลิกนัดให้ ไม่ตรวจห้องเพราะนัดไม่ได้จองห้อง ผลเป็นของตอนที่กด',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
};
