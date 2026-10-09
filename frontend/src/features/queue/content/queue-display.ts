/** The waiting-room screen. Read from across a room, so every line is short. */
export const queueDisplay = {
  metaTitle: 'จอเรียกคิว · Happy Paws Clinic',
  heading: (branch: string) => `Happy Paws Clinic · ${branch}`,
  noBranch: 'ยังไม่มีสาขาที่บัญชีนี้เข้าถึงได้',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูคิว (queue:read) ใช้บัญชีที่ดูคิวได้เพื่อเปิดจอนี้',

  calling: 'กำลังเรียก',
  callingEmpty: 'ยังไม่มีคิวที่กำลังเรียก',
  goTo: (room: string) => `เชิญที่ ${room}`,
  goToCounter: 'เชิญที่เคาน์เตอร์',

  next: 'คิวถัดไป',
  nextEmpty: 'ไม่มีคิวรอ',
  waiting: (count: number) => `รออีก ${count} คิว`,

  inRoom: 'กำลังตรวจ',
  roomFree: 'ว่าง',

  status: {
    connecting: 'กำลังเชื่อมต่อ',
    live: 'อัปเดตสด',
    reconnecting: 'หลุดการเชื่อมต่อ กำลังต่อใหม่',
  },

  error: {
    title: 'จอขาดการเชื่อมต่อชั่วคราว',
    body: 'กำลังลองใหม่อัตโนมัติ',
  },
} as const;
