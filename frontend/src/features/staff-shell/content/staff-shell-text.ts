import type { NavBadgeKey, NavCount } from '../types/staff-shell.types';

/** A count as the badge draws it: "3", or "100+" when the read stopped at its page size. */
export function countText({ count, more }: NavCount): string {
  return more ? `${count}+` : String(count);
}

const badgeWords: Record<NavBadgeKey, string> = {
  queue: 'รอเรียก',
  messages: 'รอตอบ',
  stock: 'แจ้งเตือน',
  lab: 'งานค้าง',
};

/** The words around the console's frame: the sidebar, the top bar and their menus. */
export const staffShellText = {
  /** Shown until the clinic's own name arrives, and when it cannot be read. */
  brandFallback: 'Happy Paws',
  brandTagline: 'ระบบจัดการคลินิก',

  mainMenu: 'เมนูหลัก',
  collapse: 'ย่อเมนู',
  expand: 'ขยายเมนู',
  openMenu: 'เปิดเมนู',
  closeMenu: 'ปิดเมนู',

  /** What a screen reader hears after the entry's name, e.g. "ข้อความลูกค้า, รอตอบ 3". */
  badgeLabel: (key: NavBadgeKey, count: NavCount) => `${badgeWords[key]} ${countText(count)}`,

  branch: {
    label: 'สาขา',
    none: 'ยังไม่มีสาขา',
  },

  clock: {
    label: 'เวลาคลินิก',
    open: (closes: string) => `เปิดอยู่ ปิด ${closes}`,
    /** Before opening time on a day the branch opens. */
    opensLater: (opens: string) => `ยังไม่เปิด เปิด ${opens}`,
    closed: 'ปิดแล้ว',
  },

  search: {
    label: 'ค้นหาลูกค้าและสัตว์',
    placeholder: 'ค้นหาชื่อ เบอร์ HN หรือชื่อสัตว์',
    open: 'ค้นหา',
    shortcut: '/',
    customers: 'ลูกค้า',
    pets: 'สัตว์',
    seeAll: (q: string) => `ดูผลทั้งหมดของ "${q}" ในหน้าลูกค้า`,
    searching: 'กำลังค้นหา…',
    empty: 'ไม่พบลูกค้าหรือสัตว์ที่ตรงกัน',
    tooShort: 'พิมพ์อย่างน้อย 2 ตัวอักษร',
    failed: 'ค้นหาไม่สำเร็จ ลองอีกครั้ง',
    deceased: 'เสียชีวิตแล้ว',
    microchip: 'ไมโครชิป',
    /** Read out when the list changes, so a screen reader knows what the arrows will find. */
    found: (count: number) => `พบ ${count} รายการ`,
  },

  create: {
    button: 'สร้าง',
    menu: 'สร้างรายการใหม่',
  },

  account: {
    menu: 'เมนูบัญชี',
    myAccount: 'บัญชีของฉัน',
    signOut: 'ออกจากระบบ',
  },
};
