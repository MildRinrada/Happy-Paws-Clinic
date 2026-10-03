/** Everything the portal's home page says around the visit card. Edit the wording here. */
export const portalHome = {
  metaTitle: 'หน้าแรกของฉัน',
  greeting: (name: string) => `สวัสดี คุณ${name}`,
  intro: 'ดูคิว นัด และสุขภาพของน้อง ๆ ได้จากหน้านี้',

  vaccines: {
    title: 'วัคซีนที่ใกล้ครบกำหนด',
    empty: 'ยังไม่มีวัคซีนที่ใกล้ครบกำหนด',
    overdue: 'เลยกำหนด',
    dueSoon: 'ใกล้ครบกำหนด',
    dueOn: (date: string) => `ครบกำหนด ${date}`,
  },

  pets: {
    title: 'สัตว์เลี้ยงของฉัน',
    more: 'ดูทั้งหมด',
    empty: 'ยังไม่มีสัตว์เลี้ยงในบัญชี ติดต่อคลินิกเพื่อเพิ่มน้องเข้าบัญชีนี้',
    weight: (kg: number) => `${kg} กก.`,
    deceased: 'จากไปแล้ว',
  },

  notifications: {
    title: 'แจ้งเตือนล่าสุด',
    more: 'ดูทั้งหมด',
    empty: 'ยังไม่มีแจ้งเตือน',
    unreadMark: 'ใหม่',
  },
};
