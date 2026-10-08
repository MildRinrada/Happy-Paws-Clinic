/** The overview page: title, tabs and the range picker. */
export const dashboardPage = {
  metaTitle: 'ภาพรวม · Happy Paws Clinic',
  title: 'ภาพรวม',
  subtitle: (branch: string) => `ตัวเลขของ${branch} นับตามเวลาคลินิก`,
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูภาพรวม (dashboard:read, stats:read หรือ analytics:read)',
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
    granularities: { day: 'รายวัน', week: 'รายสัปดาห์', month: 'รายเดือน' },
    note: 'ดูได้ครั้งละไม่เกิน 366 วัน',
  },

  refused: 'โหลดรายงานนี้ไม่ได้',
};
