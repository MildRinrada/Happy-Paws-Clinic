import { Scope } from '@/lib/auth/scopes';

export interface SettingsTab {
  href: string;
  label: string;
  /** The scope the page's writes need. A tab the session cannot use is not drawn. */
  scope: string;
}

/** The settings area's pages, in the order a new clinic would fill them in. */
export const settingsTabs: SettingsTab[] = [
  { href: '/settings/clinic', label: 'คลินิกและสาขา', scope: Scope.SettingsWrite },
  { href: '/settings/hours', label: 'เวลาทำการและวันหยุด', scope: Scope.SettingsWrite },
  { href: '/settings/rules', label: 'กติกาการจอง คิว และเวชระเบียน', scope: Scope.SettingsWrite },
  { href: '/settings/services', label: 'บริการและราคา', scope: Scope.ServiceManage },
  { href: '/settings/staff', label: 'พนักงานและสิทธิ์', scope: Scope.StaffRead },
  { href: '/settings/contacts', label: 'ข้อมูลติดต่อ', scope: Scope.SettingsWrite },
  { href: '/settings/line', label: 'LINE', scope: Scope.IntegrationManage },
];

export const settingsArea = {
  title: 'ตั้งค่า',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์แก้การตั้งค่าใด ๆ',
};
