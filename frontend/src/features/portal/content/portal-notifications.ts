/** Everything the notifications page says. Edit the wording here. */
export const portalNotifications = {
  title: 'แจ้งเตือน',
  intro: 'ทุกข้อความที่คลินิกส่งถึงคุณ รวมถึงข้อความที่ส่งทาง LINE หรือ SMS ไม่ถึง',

  tabs: { all: 'ทั้งหมด', unread: (count: number) => `ยังไม่อ่าน (${count})` },
  markAll: 'อ่านแล้วทั้งหมด',
  markOne: 'อ่านแล้ว',
  marking: 'กำลังบันทึก',
  unreadMark: 'ใหม่',

  empty: 'ยังไม่มีแจ้งเตือน',
  emptyUnread: 'อ่านครบทุกข้อความแล้ว',

  older: 'ดูข้อความเก่ากว่า',
  newest: 'กลับไปข้อความล่าสุด',
  failed: 'บันทึกไม่สำเร็จ กรุณาลองใหม่อีกครั้ง',
};
