/** "ต้องจัดการตอนนี้": the work waiting on the clinic, each line a link to where it is done. */
export const attentionBox = {
  heading: 'ต้องจัดการตอนนี้',
  allClear: 'เรียบร้อยดี ไม่มีงานค้าง',
  partly: 'บางรายการตรวจไม่ได้ในตอนนี้ จึงไม่ได้แสดง',
  /** A count read off a list that hit its page size is a floor. */
  count: (count: string, more: boolean) => (more ? `${count}+` : count),

  rows: {
    long_wait: (minutes: number) => `คิวที่รอเรียกเกิน ${minutes} นาที`,
    messages: 'ข้อความลูกค้าที่รอตอบ',
    reviews: 'รีวิวที่รอตรวจ',
    stock: 'ยาที่หมด หมดอายุ หรือใกล้หมดอายุ',
    attendance: 'นัดวันนี้ที่เสี่ยงไม่มา',
  },

  longest: (minutes: number) => `รอนานสุด ${minutes} นาที`,
  stock: { out: 'หมด', expired: 'หมดอายุ', near: 'ใกล้หมดอายุ' },
  attendance: 'โทรยืนยันกับเจ้าของก่อนถึงเวลา',
};
