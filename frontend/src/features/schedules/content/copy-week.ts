/** The "คัดลอกเวรสัปดาห์ก่อน" dialog on the roster (docs/contract-extensions.md § 49). Edit the wording here. */
export const copyWeekText = {
  summary: (shifts: number, people: number, from: string, to: string) =>
    `คัดลอกเวร ${shifts} เวรของ ${people} คน จากสัปดาห์ ${from} มาลงสัปดาห์ ${to} ในเวลาเดิมของแต่ละวัน`,
  nothing: 'สัปดาห์ก่อนไม่มีเวรที่สาขานี้ให้คัดลอก',
  rules: [
    'ถ้ามีเวรใดชนกับเวรหรือวันลาที่มีอยู่แล้ว ระบบจะไม่คัดลอกเลยสักเวร และแสดงรายการที่ชน',
    'เวรของวันที่ผ่านไปแล้วจะไม่ถูกคัดลอก',
    'คัดลอกได้ครั้งละไม่เกิน 200 ช่วงเวร',
  ],
  submit: 'คัดลอก',
  submitting: 'กำลังคัดลอก…',
  cancel: 'ยกเลิก',
  done: (count: number) => `คัดลอกแล้ว ${count} ช่วงเวร`,
  skipped: (count: number) => `ข้ามเวรของวันที่ผ่านไปแล้ว ${count} ช่วง`,
  clashes: 'ไม่ได้คัดลอกเลย เพราะบางเวรชนกับของที่มีอยู่แล้วในสัปดาห์นี้',
  clashShift: 'เวรเดิม',
  clashLeave: 'วันลา',
};
