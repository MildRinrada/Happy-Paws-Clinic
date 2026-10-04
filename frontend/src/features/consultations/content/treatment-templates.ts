/** The treatment-template picker on the case's procedures form. */
export const treatmentTemplates = {
  open: 'เลือกจากเทมเพลต',
  close: 'ปิดเทมเพลต',
  loading: 'กำลังโหลดเทมเพลต…',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  retry: 'ลองอีกครั้ง',
  none: 'คลินิกยังไม่มีเทมเพลต',
  label: 'เทมเพลต',
  choose: 'เลือกเทมเพลต',
  anySpecies: 'ทุกชนิด',
  option: (name: string, species: string) => `${name} (${species})`,
  course: (sessions: number, interval?: number) =>
    interval ? `คอร์ส ${sessions} ครั้ง ห่างกัน ${interval} วัน` : `คอร์ส ${sessions} ครั้ง`,
  procedures: 'หัตถการ',
  use: 'ใส่ในฟอร์ม',
  quantity: (n: number) => `x${n}`,
  medicines: 'ยาในเทมเพลต (สั่งในส่วนใบสั่งยา)',
  medicine: (dose: string, frequency: string, days: number, quantity: number) =>
    `${dose} ${frequency} ${days} วัน (จ่าย ${quantity})`,
  plan: 'แผน',
  hint: 'เลือกหัตถการแล้วใส่ราคาเอง ฟอร์มยังไม่บันทึกจนกว่าจะกดเพิ่ม',
};
