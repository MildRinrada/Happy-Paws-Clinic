/** The reports page: title, tabs and the range picker. */
export const reportsPage = {
  metaTitle: 'รายงาน · Happy Paws Clinic',
  title: 'รายงาน',
  subtitle: (branch: string) => `ตัวเลขของ${branch} ตามช่วงวันที่ที่เลือก นับตามเวลาคลินิก`,
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูรายงาน (stats:read, analytics:read หรือ analytics:hr)',
  noBranch: 'บัญชีนี้ยังไม่ได้ประจำสาขาใด',
  minutes: 'นาที',

  tabs: {
    label: 'เลือกรายงาน',
    operations: 'สถิติการทำงาน',
    business: 'ธุรกิจ',
  },

  range: {
    heading: 'ช่วงวันที่',
    from: 'ตั้งแต่',
    to: 'ถึง',
    granularity: 'แบ่งกราฟเป็น',
    apply: 'แสดง',
    presets: {
      last7: '7 วัน',
      last30: '30 วัน',
      last90: '90 วัน',
      thisMonth: 'เดือนนี้',
    } as Record<string, string>,
    presetsLabel: 'ช่วงที่ใช้บ่อย',
    granularities: { day: 'รายวัน', week: 'รายสัปดาห์', month: 'รายเดือน' },
    note: 'ดูได้ครั้งละไม่เกิน 366 วัน',
  },

  chartMax: (value: string) => `สูงสุด ${value}`,
  refused: 'โหลดรายงานนี้ไม่ได้',
};
