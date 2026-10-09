import type { PackageStatus } from '../types/package.types';

/** Everything the "แพ็กเกจของฉัน" page says. Edit the wording here. */
export const portalPackages = {
  title: 'แพ็กเกจของฉัน',
  intro: 'แพ็กเกจที่ซื้อไว้กับคลินิก ใช้ไปแล้วกี่ครั้ง เหลืออีกกี่ครั้ง และใช้ได้ถึงวันไหน',

  usable: 'ใช้ได้อยู่',
  finished: 'ใช้ครบ หมดอายุ หรือยกเลิกแล้ว',

  status: {
    pending_payment: 'รอชำระเงิน',
    active: 'ใช้ได้',
    used_up: 'ใช้ครบแล้ว',
    expired: 'หมดอายุ',
    cancelled: 'ยกเลิกแล้ว',
  } satisfies Record<PackageStatus, string>,

  remaining: (left: number, total: number) => `เหลือ ${left} จาก ${total} ครั้ง`,
  forPet: (name: string) => `สำหรับน้อง${name}`,
  anyPet: 'ใช้ได้กับน้องทุกตัวในบัญชี',
  services: (names: string) => `ใช้กับ ${names}`,
  expires: (date: string) => `ใช้ได้ถึง ${date}`,
  expiresSoon: 'ใกล้หมดอายุ',
  noExpiry: 'ไม่มีวันหมดอายุ',
  purchased: (date: string) => `ซื้อเมื่อ ${date}`,

  empty: {
    title: 'ยังไม่มีแพ็กเกจ',
    body: 'แพ็กเกจอย่างวัคซีนรายปี หรืออาบน้ำตัดขน 10 ครั้ง จะขึ้นที่นี่เมื่อซื้อกับคลินิก พร้อมจำนวนครั้งที่เหลือและวันหมดอายุ',
    ask: 'สนใจแพ็กเกจ สอบถามได้ที่เคาน์เตอร์ตอนพาน้องมา หรือติดต่อคลินิก',
    contact: { label: 'ติดต่อคลินิก', href: '/contact' },
    book: 'จองนัด',
  },
};
