/** The "สถิติการทำงาน" tab: appointments, queue and workload. */
export const operationsReport = {
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูสถิติการทำงาน (stats:read)',

  appointments: {
    title: 'นัดหมาย',
    total: 'นัดทั้งหมด',
    completed: 'มาตรวจครบ',
    cancelled: 'ยกเลิก',
    cancellationRate: 'อัตรายกเลิก',
    noShow: 'ไม่มาตามนัด',
    noShowRate: 'อัตราไม่มา',
    walkIn: 'walk-in',
    bookedOnline: 'จองออนไลน์',
    lateCancelled: 'ยกเลิกกระชั้นชิด',
    chartLabel: 'จำนวนนัดในแต่ละช่วง',
    noShowRateHint: 'นับจากนัดที่ไม่ได้ถูกยกเลิก',
  },

  queue: {
    title: 'คิว',
    tickets: 'บัตรคิว',
    served: 'ตรวจเสร็จ',
    averageWait: 'รอเฉลี่ย',
    averageRoom: 'อยู่ในห้องเฉลี่ย',
    outstanding: 'คิวค้าง',
    outstandingHint: 'บัตรที่ยังไม่จบ ทั้งรอเรียก ถูกเรียก และอยู่ในห้อง',
    peakHour: 'ชั่วโมงพีค',
    chartLabel: 'จำนวนบัตรคิวตามชั่วโมงที่ออก',
  },

  workload: {
    title: 'ภาระงานของสัตวแพทย์',
    cases: 'เคสที่ปิด',
    averageCase: 'เวลาเฉลี่ยต่อเคส',
    openCases: 'เคสเปิดค้างตอนนี้',
    unassigned: 'คิววันนี้ที่ยังไม่มีหมอ',
    columns: {
      name: 'สัตวแพทย์',
      cases: 'เคสที่ปิด',
      averageCase: 'เฉลี่ยต่อเคส',
      appointments: 'นัด',
      openCases: 'เคสค้าง',
      waitingQueue: 'คิวรอวันนี้',
    },
    empty: 'ยังไม่มีเคสหรือคิวในช่วงนี้',
    note: 'เคสค้างและคิวรอเป็นตัวเลขของตอนนี้ ไม่ขึ้นกับช่วงวันที่',
  },
};
