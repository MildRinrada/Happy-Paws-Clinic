/** The four numbers of the day and the "my work" strip above them. */
export const headlineTiles = {
  heading: 'วันนี้',
  updatedAt: (time: string, seconds: number) => `อัปเดตเมื่อ ${time} น. หน้านี้อัปเดตเองทุก ${seconds / 60} นาที`,
  label: 'ตัวเลขหลักของวันนี้',

  appointments: 'นัดวันนี้',
  waiting: 'รอเรียก',
  inRoom: 'อยู่ในห้องตรวจ',
  revenue: 'รายรับวันนี้ (สุทธิ)',

  upcoming: (count: string) => `ยังไม่มา ${count}`,
  longest: (wait: string) => `รอนานสุด ${wait}`,
  served: (count: string) => `ตรวจเสร็จ ${count}`,
  payments: (count: string) => `${count} รายการ`,

  /** Against the same weekday last week, which is a whole day, as today's total is. */
  vsLastWeek: (difference: number) =>
    difference === 0 ? 'เท่ากับสัปดาห์ก่อน' : `${difference > 0 ? '+' : ''}${difference} จากสัปดาห์ก่อน`,
  /** Today's takings are still coming in, so last week's whole day is a reference, not a percentage. */
  lastWeekRevenue: (amount: string) => `สัปดาห์ก่อนทั้งวัน ${amount}`,
};
