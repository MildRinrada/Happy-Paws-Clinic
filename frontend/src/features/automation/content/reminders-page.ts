/** The upcoming reminders page. */
export const remindersPage = {
  metaTitle: 'ข้อความเตือนที่จะส่ง · Happy Paws Clinic',
  title: 'ข้อความเตือนที่จะส่ง',
  subtitle: (days: number) => `ข้อความที่กติกาซึ่งเปิดใช้อยู่จะส่งใน ${days} วันข้างหน้า คำนวณจากข้อมูลล่าสุดทุกครั้งที่เปิดหน้า`,
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูข้อความเตือน',
  empty: 'ยังไม่มีข้อความเตือนที่จะส่งในช่วงนี้',
  truncated: 'แสดงเฉพาะ 500 รายการแรก',
  days: 'ดูล่วงหน้า',
  dayChoices: [1, 3, 7, 14, 30],
  dayLabel: (days: number) => `${days} วัน`,
  apply: 'แสดง',
  columns: {
    sendAt: 'ส่งเมื่อ',
    customer: 'ลูกค้าและสัตว์',
    subject: 'เรื่อง',
    message: 'ข้อความ',
    status: 'สถานะ',
  },
  dueNow: 'ถึงเวลาแล้ว ส่งในรอบถัดไป',
  rule: (name: string) => `กติกา ${name}`,
  skipReasons: {
    no_consent: 'ลูกค้าไม่ได้ยินยอมรับข้อความชนิดนี้',
  } as Record<string, string>,
};
