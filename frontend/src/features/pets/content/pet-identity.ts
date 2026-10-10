/**
 * Sex, birth date, age and neuter status, wherever a pet's card shows or edits them: the staff
 * profile, the register form, the edit form and the portal. Not in the API reference; see
 * docs/contract-extensions.md § 28.
 */
export const petIdentity = {
  sex: {
    label: 'เพศ',
    options: { unknown: 'ไม่ทราบ', male: 'เพศผู้', female: 'เพศเมีย' } as Record<string, string>,
  },
  neutered: {
    label: 'ทำหมัน',
    options: { unknown: 'ไม่ทราบ', yes: 'ทำหมันแล้ว', no: 'ยังไม่ทำหมัน' } as Record<string, string>,
    closedHint: 'สัตว์ที่บันทึกว่าเสียชีวิตแล้วเปลี่ยนสถานะทำหมันไม่ได้',
  },
  birthDate: {
    label: 'วันเกิด',
    hint: 'ไม่ทราบให้เว้นว่าง',
    estimated: 'เป็นวันเกิดโดยประมาณ',
    estimatedHint: 'เช่น ประเมินจากฟันของสัตว์ที่รับมาเลี้ยงตอนโตแล้ว',
    future: 'วันเกิดต้องไม่อยู่ในอนาคต',
    about: (date: string) => `ประมาณ ${date}`,
  },
  age: {
    label: 'อายุ',
    value: (years: number, months: number, estimated: boolean) => {
      const parts = [years > 0 ? `${years} ปี` : '', months > 0 ? `${months} เดือน` : ''].filter(Boolean);
      const text = parts.length > 0 ? parts.join(' ') : 'ไม่ถึงหนึ่งเดือน';
      return estimated ? `ประมาณ ${text}` : text;
    },
  },
};
