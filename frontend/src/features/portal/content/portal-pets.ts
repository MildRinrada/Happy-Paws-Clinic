/** Everything the "สัตว์เลี้ยงของฉัน" page says. Edit the wording here. */
export const portalPets = {
  title: 'สัตว์เลี้ยงของฉัน',
  intro: 'ข้อมูลที่คลินิกบันทึกไว้ และข้อมูลที่คุณเล่าให้คลินิกรู้',
  empty: 'ยังไม่มีสัตว์เลี้ยงในบัญชี ติดต่อคลินิกเพื่อเพิ่มน้องเข้าบัญชีนี้',
  deceased: 'จากไปแล้ว',
  nickname: (value: string) => `ชื่อเล่น ${value}`,

  facts: {
    hospitalNumber: 'เลข HN',
    microchip: 'ไมโครชิป',
    weight: 'น้ำหนักล่าสุด',
    weightValue: (kg: number, date: string) => `${kg} กก. (${date})`,
    diet: 'อาหาร',
    behaviour: 'พฤติกรรม',
  },

  allergiesTitle: 'การแพ้',
  noAllergies: 'ไม่มีประวัติแพ้',
  severity: { mild: 'เล็กน้อย', moderate: 'ปานกลาง', severe: 'รุนแรง' } as Record<string, string>,

  records: 'ดูประวัติการรักษา',
  edit: 'แก้ไขข้อมูล',
};
