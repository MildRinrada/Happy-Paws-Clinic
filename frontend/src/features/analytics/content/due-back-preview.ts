/** "ลูกค้าที่ถึงเวลากลับมา": the first few customers to call, from the CRM call list. */
export const dueBackPreview = {
  heading: 'ลูกค้าที่ถึงเวลากลับมา',
  hint: 'สัตว์ที่วัคซีนเลยกำหนดและยังไม่มีนัด เรียงจากที่เพิ่งเลยกำหนดก่อน',
  empty: 'ตอนนี้ไม่มีสัตว์ที่วัคซีนเลยกำหนด',
  all: 'ดูรายชื่อทั้งหมด',
  lapsed: 'ลูกค้าที่หายไปนานกว่าปกติ',
  overdue: (days: number) => `เลยมา ${days} วัน`,
};
