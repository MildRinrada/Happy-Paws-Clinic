import { Scope } from '@/lib/auth/scopes';

export interface SettingsTab {
  href: string;
  label: string;
  /** The scope the page's writes need. A tab the session cannot use is not drawn. */
  scope: string;
}

export interface SettingsGroup {
  key: string;
  label: string;
  tabs: SettingsTab[];
}

/** The settings area's pages by subject, in the order a new clinic would fill them in. */
export const settingsGroups: SettingsGroup[] = [
  {
    key: 'clinic',
    label: 'คลินิก',
    tabs: [
      { href: '/settings/clinic', label: 'คลินิกและสาขา', scope: Scope.SettingsWrite },
      { href: '/settings/hours', label: 'เวลาทำการและวันหยุด', scope: Scope.SettingsWrite },
      { href: '/settings/contacts', label: 'ข้อมูลติดต่อ', scope: Scope.SettingsWrite },
    ],
  },
  {
    key: 'care',
    label: 'การจองและการรักษา',
    tabs: [
      { href: '/settings/rules', label: 'กติกาการจอง คิว และเวชระเบียน', scope: Scope.SettingsWrite },
      { href: '/settings/appointments', label: 'นโยบายยกเลิกและประเภทนัด', scope: Scope.SettingsWrite },
      { href: '/settings/services', label: 'บริการและราคา', scope: Scope.ServiceManage },
    ],
  },
  {
    key: 'team',
    label: 'ทีมงาน',
    tabs: [{ href: '/settings/staff', label: 'พนักงานและสิทธิ์', scope: Scope.StaffRead }],
  },
  {
    key: 'customers',
    label: 'ลูกค้าและเว็บไซต์',
    tabs: [
      { href: '/settings/loyalty', label: 'แต้มสะสม', scope: Scope.SettingsWrite },
      { href: '/settings/reviews', label: 'รีวิวจากลูกค้า', scope: Scope.ContentManage },
      { href: '/settings/articles', label: 'บทความ', scope: Scope.ContentManage },
      { href: '/settings/privacy-policy', label: 'ประกาศความเป็นส่วนตัว', scope: Scope.SettingsRead },
    ],
  },
  {
    key: 'connections',
    label: 'เชื่อมต่อ',
    tabs: [
      { href: '/settings/line', label: 'LINE', scope: Scope.IntegrationManage },
      { href: '/settings/integrations', label: 'เชื่อมต่อระบบ', scope: Scope.IntegrationManage },
    ],
  },
];

/** Every page in one list, group by group: what `/settings` and the staff menu open first. */
export const settingsTabs: SettingsTab[] = settingsGroups.flatMap((group) => group.tabs);

export const settingsArea = {
  title: 'ตั้งค่า',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์แก้การตั้งค่าใด ๆ',
};

/** The words of the settings navigation itself. */
export const settingsNavText = {
  label: 'หมวดการตั้งค่า',
  /** The phone's page switcher. */
  switcher: 'หน้าตั้งค่า',
  /** Read with a page that still needs setting up, and written after its name in the phone's list. */
  attention: 'ต้องตั้งค่า',
};
