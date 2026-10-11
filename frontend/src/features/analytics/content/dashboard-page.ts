/** The overview page: what needs doing now, today's numbers, and the shortcuts of the counter. */
export const dashboardPage = {
  metaTitle: 'ภาพรวม · Happy Paws Clinic',
  title: 'ภาพรวม',
  subtitle: (branch: string) => `งานและตัวเลขของ${branch} วันนี้ นับตามเวลาคลินิก`,
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูภาพรวม (dashboard:read)',
  noBranch: 'บัญชีนี้ยังไม่ได้ประจำสาขาใด',
};
