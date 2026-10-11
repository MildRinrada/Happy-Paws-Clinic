/** Everything the "สัตว์เลี้ยงของฉัน" pages say, around the list and on each pet's card. Edit the wording here. */
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
    /** A fact the clinic or the owner has not filled in yet. */
    missing: 'ยังไม่มีข้อมูล',
  },

  /** The line under the name: "สุนัข พันธุ์ปอมเมอเรเนียน เพศเมีย 3 ปี". */
  describe: {
    breed: (breed: string) => `พันธุ์${breed}`,
    years: (count: number) => `${count} ปี`,
    months: (count: number) => `${count} เดือน`,
    newborn: 'ไม่ถึงหนึ่งเดือน',
    about: (age: string) => `ประมาณ ${age}`,
    neutered: 'ทำหมันแล้ว',
  },

  allergiesTitle: 'การแพ้',
  noAllergies: 'ไม่มีประวัติแพ้',
  severity: { mild: 'เล็กน้อย', moderate: 'ปานกลาง', severe: 'รุนแรง' } as Record<string, string>,
  /** "ไก่ ปานกลาง" on the red tag. */
  allergy: (substance: string, severity: string) => `${substance} ${severity}`,

  records: 'ดูประวัติการรักษา',
  edit: 'แก้ไขข้อมูล',
  documents: 'เอกสารของน้อง',

  /** The list of pets, on a phone. */
  list: {
    weight: (kg: number) => `${kg} กก.`,
    open: (name: string) => `ดูข้อมูลของน้อง${name}`,
    searchLabel: 'ค้นหาน้อง',
    searchPlaceholder: 'ค้นจากชื่อหรือชื่อเล่น',
    found: (count: number) => `พบ ${count} ตัว`,
    noMatch: 'ไม่พบน้องที่ชื่อนี้',
    deceasedGroup: (count: number) => `น้องที่จากไปแล้ว (${count})`,
    hint: {
      overdue: (vaccine: string) => `วัคซีน${vaccine}เลยกำหนด`,
      dueSoon: (vaccine: string) => `วัคซีน${vaccine}ใกล้ครบกำหนด`,
      /** When more than one vaccine is due, the first and how many more. */
      more: (text: string, count: number) => `${text} และอีก ${count} รายการ`,
    },
  },
};
