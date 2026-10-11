import type { SiteLink } from '@/config/site-navigation';

/**
 * The portal's menu, and the other links in its header. Edit them here, not in the components.
 *
 * `short` is the label under the icon in the phone's bottom bar, where the full one does not fit.
 */

export type PortalIcon = 'home' | 'paw' | 'calendar' | 'records';

export interface PortalNavLink extends SiteLink {
  short: string;
  icon: PortalIcon;
}

export const PORTAL_HOME_HREF = '/portal';

export const portalPaths = {
  book: '/portal/book',
  /** The booking form with one pet already chosen. */
  bookFor: (petId: string) => `/portal/book?pet=${encodeURIComponent(petId)}`,
  notifications: '/portal/notifications',
  messages: '/portal/messages',
  account: '/portal/account',
  pets: '/portal/pets',
  pet: (petId: string) => `/portal/pets/${encodeURIComponent(petId)}`,
  editPet: (petId: string) => `/portal/pets/${encodeURIComponent(petId)}/edit`,
  petDocuments: (petId: string) => `/portal/pets/${encodeURIComponent(petId)}/documents`,
  packages: '/portal/packages',
  invoices: '/portal/invoices',
  appointments: '/portal/appointments',
  reviews: '/portal/reviews',
  referral: '/portal/referral',
  reschedule: (appointmentId: string) => `/portal/appointments/${encodeURIComponent(appointmentId)}/reschedule`,
};

export const portalNavLinks: PortalNavLink[] = [
  { label: 'หน้าแรก', short: 'หน้าแรก', href: PORTAL_HOME_HREF, icon: 'home' },
  { label: 'สัตว์เลี้ยงของฉัน', short: 'สัตว์เลี้ยง', href: portalPaths.pets, icon: 'paw' },
  { label: 'นัดหมาย', short: 'นัดหมาย', href: portalPaths.appointments, icon: 'calendar' },
  { label: 'ประวัติการรักษา', short: 'ประวัติ', href: '/portal/records', icon: 'records' },
];

export const portalHeader = {
  navLabel: 'เมนูของฉัน',
  book: { label: 'จองนัด', href: portalPaths.book } satisfies SiteLink,
  notifications: { label: 'แจ้งเตือน', href: portalPaths.notifications } satisfies SiteLink,
  unread: (count: number) => `ยังไม่ได้อ่าน ${count} รายการ`,
  accountMenuLabel: 'เมนูบัญชี',
  accountLinks: [
    { label: 'บัญชีของฉัน', href: portalPaths.account },
    { label: 'แพ็กเกจของฉัน', href: portalPaths.packages },
    { label: 'บิลและใบเสร็จ', href: portalPaths.invoices },
    { label: 'แชทกับคลินิก', href: portalPaths.messages },
    { label: 'รีวิวการมาใช้บริการ', href: portalPaths.reviews },
    { label: 'ชวนเพื่อน', href: portalPaths.referral },
    { label: 'แจ้งเตือน', href: portalPaths.notifications },
    { label: 'หน้าเว็บคลินิก', href: '/' },
  ] satisfies SiteLink[],
  signOut: 'ออกจากระบบ',
};
