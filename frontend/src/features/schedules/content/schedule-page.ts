/** The staff roster page. */
export const schedulePage = {
  metaTitle: 'ตารางเวร · Happy Paws Clinic',
  title: 'ตารางเวร',
  subtitle: (branch: string) => `เวรของพนักงานที่${branch} เวลาที่ลูกค้าจองได้มาจากตารางนี้`,
  noAccess: 'บัญชีนี้ดูตารางเวรไม่ได้ (schedule:read)',
  noBranch: 'บัญชีนี้ยังไม่ได้ประจำสาขาใด',
  empty: 'สัปดาห์นี้ยังไม่มีใครลงเวรที่สาขานี้',
  person: 'พนักงาน',
  leave: 'ลา',
  me: 'คุณ',
  today: 'วันนี้',
  hours: 'รวม',
  hoursValue: (hours: number) => `${hours} ชม.`,
  hoursHint: 'ชั่วโมงทำงานทั้งสัปดาห์ ไม่นับเวลาพัก',
  breakNote: (start: string, end: string) => `พัก ${start}-${end}`,
  addHere: (name: string, day: string) => `ลงเวรให้ ${name} ${day}`,
  editHere: (name: string, hours: string) => `แก้หรือลบเวร ${hours} ของ ${name}`,

  actions: {
    addShift: '+ ลงเวร',
    timeOff: 'บันทึกวันลา',
    copyWeek: 'คัดลอกเวรสัปดาห์ก่อน',
  },

  dialogs: {
    addShift: 'ลงเวร',
    timeOff: 'บันทึกวันลา',
    editShift: 'แก้หรือลบเวร',
    copyWeek: 'คัดลอกเวรสัปดาห์ก่อน',
    close: 'ปิด',
  },

  groups: {
    vets: 'สัตวแพทย์',
    frontDesk: 'ต้อนรับ',
    assistants: 'ผู้ช่วยสัตวแพทย์',
    others: 'ฝ่ายอื่น',
  },

  cover: {
    heading: 'คนเข้าเวร',
    vets: (count: number) => `หมอ ${count}`,
    frontDesk: (count: number) => `ต้อนรับ ${count}`,
    noVet: 'ไม่มีหมอ',
    busy: 'คาดคนไข้มาก',
    busyHint: 'การคาดการณ์ความหนาแน่นบอกว่าวันนี้มีบางชั่วโมงที่คนไข้ต่อหมอสูงกว่าปกติ หรือมีคนไข้แต่ไม่มีหมอ',
  },

  dayView: {
    pick: 'เลือกวัน',
    nobody: 'วันนี้ยังไม่มีใครเข้าเวรที่สาขานี้',
  },

  week: {
    previous: '← สัปดาห์ก่อน',
    next: 'สัปดาห์ถัดไป →',
    thisWeek: 'สัปดาห์นี้',
    jump: 'ไปสัปดาห์ของวันที่',
  },

  shift: {
    staff: 'พนักงาน',
    choose: 'เลือกพนักงาน',
    date: 'วันที่ (เวรแรก)',
    start: 'เริ่ม',
    end: 'เลิก',
    withBreak: 'มีเวลาพัก',
    breakStart: 'พักตั้งแต่',
    breakEnd: 'ถึง',
    repeat: 'ลงซ้ำทุกสัปดาห์',
    repeatDays: 'วันที่ทำงาน',
    until: 'จนถึงวันที่',
    repeatHint: 'สร้างได้ครั้งละไม่เกิน 200 เวร ภายใน 1 ปีนับจากเวรแรก',
    pickDay: 'เลือกอย่างน้อยหนึ่งวัน',
    submit: 'ลงเวร',
    submitting: 'กำลังลงเวร…',
  },

  timeOff: {
    from: 'ลาตั้งแต่',
    to: 'ถึง',
    reason: 'เหตุผล (ไม่บังคับ)',
    reasonPlaceholder: 'เช่น ลาป่วย ลาพักร้อน',
    submit: 'บันทึกวันลา',
    submitting: 'กำลังบันทึก…',
    note: 'มีผลทันที เวรที่ทับช่วงลาจะถูกตัดออก นัดที่จองไว้แล้วไม่ถูกย้าย ระบบจะแสดงรายการให้โทรเลื่อนหรือเปลี่ยนหมอ',
    result: (cancelled: number) => `บันทึกวันลาแล้ว ตัดเวรที่ทับออก ${cancelled} เวร`,
    noConflicts: 'ไม่มีนัดที่จองไว้ในช่วงลา',
    conflicts: 'นัดที่จองไว้ในช่วงลา ต้องโทรเลื่อนหรือเปลี่ยนหมอ',
    suggested: 'หมอที่รับแทนได้',
    nobody: 'ไม่มีหมอว่างที่รับแทนได้',
    openAppointment: 'ดูนัด',
  },
};
