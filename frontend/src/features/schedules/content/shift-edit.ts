/** The "แก้หรือลบเวร" dialog on the roster (docs/contract-extensions.md § 47). Edit the wording here. */
export const shiftEditText = {
  who: (name: string, day: string) => `${name} ${day}`,
  blocks: 'เวรนี้เก็บเป็นสองช่วง คือก่อนพักและหลังพัก เลือกช่วงที่จะแก้หรือลบ',
  block: (index: number, hours: string) => `ช่วงที่ ${index} ${hours}`,
  start: 'เริ่ม',
  end: 'เลิก',
  started: 'เวรช่วงนี้เริ่มไปแล้ว แก้ได้แค่เวลาเลิก และลบไม่ได้ ถ้าต้องหยุดก่อนเวลา ให้แก้เวลาเลิกหรือบันทึกวันลา',

  save: 'บันทึกเวลาใหม่',
  saving: 'กำลังบันทึก…',
  saved: 'บันทึกเวลาใหม่แล้ว',

  remove: 'ลบช่วงนี้',
  removeSingle: 'ลบเวรนี้',
  removeAll: (count: number) => `ลบทั้งเวร (${count} ช่วง)`,
  confirm: 'ยืนยันการลบ',
  keep: 'ไม่ลบ',
  removing: 'กำลังลบ…',
  removed: 'ลบเวรแล้ว',
  removeNote: 'ลบแล้วลูกค้าจองเวลานี้ไม่ได้อีก นัดที่จองไว้แล้วไม่ถูกย้าย ระบบจะแสดงรายการให้โทรเลื่อนหรือเปลี่ยนหมอ',

  conflicts: 'นัดที่จองไว้ในเวลาที่ตัดออก ต้องโทรเลื่อนหรือเปลี่ยนหมอ',
  noConflicts: 'ไม่มีนัดที่จองไว้ในเวลาที่ตัดออก',
};
