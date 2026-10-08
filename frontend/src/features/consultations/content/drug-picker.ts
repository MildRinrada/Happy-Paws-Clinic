import { formatBaht } from '@/lib/format/money';

/** The drug search on the prescription form (docs/contract-extensions.md § 20). */
export const drugPicker = {
  hintEmpty: 'พิมพ์ชื่อยาเพื่อค้นจากคลัง หรือพิมพ์ชื่อเองก็ได้',
  freeText: 'ไม่ผูกกับสต๊อก ตอนจ่ายจะจับคู่ด้วยชื่อ ถ้าชื่อไม่ตรงกับในคลังทุกตัวอักษร จะไม่ตัดสต๊อกและไม่คิดเงิน',
  linked: (name: string, unit: string, priceSatang: number) =>
    `ผูกกับสต๊อก: ${name} ราคา ${formatBaht(priceSatang)} ต่อ${unit} ตัดสต๊อกและคิดเงินตอนจ่าย`,
  searching: 'กำลังค้นในคลัง…',
  noMatch: 'ไม่พบยาชื่อนี้ในคลัง ใช้ชื่อที่พิมพ์ได้ แต่จะไม่ผูกกับสต๊อก',
  failed: 'ค้นคลังยาไม่ได้ ใช้ชื่อที่พิมพ์ได้ แต่จะไม่ผูกกับสต๊อก',
  listLabel: 'ยาในคลัง',
  quantityIn: (unit: string) => `จำนวนที่จ่าย (${unit})`,
  price: (priceSatang: number, unit: string) => `${formatBaht(priceSatang)} ต่อ${unit}`,
  available: (count: number, unit: string) => `เหลือ ${count} ${unit}`,
  status: {
    ok: 'มีของ',
    low: 'ใกล้หมด',
    out: 'หมด',
  },
  list: {
    linked: 'ผูกกับสต๊อก',
    unlinked: 'ไม่ผูกกับสต๊อก',
  },
};
