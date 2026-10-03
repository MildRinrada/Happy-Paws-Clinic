/** The counter's "add a pet" form on a customer's page. */
export const registerPet = {
  title: 'เพิ่มสัตว์เลี้ยง',
  hint: 'ระบบออกเลข HN ให้อัตโนมัติเมื่อบันทึก',
  fields: {
    name: 'ชื่อสัตว์',
    species: 'ชนิด',
    otherSpecies: 'ระบุชนิด',
    breed: 'สายพันธุ์ (ไม่บังคับ)',
    breedPlaceholder: 'เช่น ชิสุ, ไทยหลังอาน, เปอร์เซีย',
    microchip: 'เลขไมโครชิป (ไม่บังคับ)',
  },
  /** What the counter picks from. "อื่น ๆ" opens a box: the API takes any species name. */
  species: ['สุนัข', 'แมว', 'กระต่าย', 'นก', 'หนูแฮมสเตอร์'],
  otherOption: 'อื่น ๆ',
  submit: 'บันทึกสัตว์เลี้ยง',
  submitting: 'กำลังบันทึก…',
  cancel: 'ยกเลิก',
  saved: (name: string, hn: string) => `เพิ่ม ${name} แล้ว ได้เลข ${hn}`,
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  noAccess: 'บัญชีนี้เพิ่มสัตว์เลี้ยงไม่ได้ (patient:write)',
};
