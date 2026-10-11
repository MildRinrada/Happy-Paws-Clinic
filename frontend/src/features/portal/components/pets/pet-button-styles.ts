/** The pet pages' two button looks: an outlined white one, and the solid green one for booking. */
const base =
  'inline-flex min-w-0 items-center justify-center whitespace-nowrap rounded-xl px-3 text-center font-display font-medium transition';

/** "แก้ไขข้อมูล", "เอกสารของน้อง", "ดูประวัติการรักษา". 46px on a phone, 40px from 1024px, as drawn. */
export const outlineButton = `${base} h-[46px] border border-ink-200 bg-white text-[15px] text-ink-900 hover:bg-ink-50 lg:h-10 lg:px-2 lg:text-[13.5px]`;

/** "จองนัดฉีดพิษสุนัขบ้า", full width under the vaccines. */
export const solidButton = `${base} h-11 w-full bg-brand-600 text-[15px] font-semibold text-white hover:bg-brand-700`;
