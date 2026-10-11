import { portalPaths } from './portal-navigation';

/** The three shortcuts under the visit card, each with its own little drawing. */
export type QuickActionIcon = 'book' | 'bills' | 'packages';

/** Everything the portal's home page says around the visit card. Edit the wording here. */
export const portalHome = {
  metaTitle: 'หน้าแรกของฉัน',
  greeting: (name: string) => `สวัสดี คุณ${name}`,
  intro: 'ดูคิว นัด และสุขภาพของน้อง ๆ ได้จากหน้านี้',

  quickActions: {
    label: 'ทางลัด',
    items: [
      { label: 'จองนัด', href: portalPaths.book, icon: 'book' },
      { label: 'บิลและใบเสร็จ', href: portalPaths.invoices, icon: 'bills' },
      { label: 'แพ็กเกจของฉัน', href: portalPaths.packages, icon: 'packages' },
    ] satisfies { label: string; href: string; icon: QuickActionIcon }[],
  },

  vaccines: {
    title: 'วัคซีนที่ใกล้ครบกำหนด',
    empty: 'ยังไม่มีวัคซีนที่ใกล้ครบกำหนด',
    overdue: 'เลยกำหนด',
    dueSoon: 'ใกล้ครบกำหนด',
    /** "พิษสุนัขบ้า น้องโมจิ". */
    forPet: (vaccine: string, pet: string) => `${vaccine} น้อง${pet}`,
    dueOn: (date: string) => `ครบกำหนด ${date}`,
  },

  pets: {
    title: 'สัตว์เลี้ยงของฉัน',
    more: 'ดูทั้งหมด',
    empty: 'ยังไม่มีสัตว์เลี้ยงในบัญชี ติดต่อคลินิกเพื่อเพิ่มน้องเข้าบัญชีนี้',
    weight: (kg: number) => `${kg} กก.`,
    deceased: 'จากไปแล้ว',
  },

  notifications: {
    title: 'แจ้งเตือนล่าสุด',
    more: 'ดูทั้งหมด',
    empty: 'ยังไม่มีแจ้งเตือน',
    unreadMark: 'ใหม่',
    /** When a message came, in words for the last two days: "วันนี้ 08:12", "เมื่อวาน 16:40". */
    today: (time: string) => `วันนี้ ${time}`,
    yesterday: (time: string) => `เมื่อวาน ${time}`,
  },
};
