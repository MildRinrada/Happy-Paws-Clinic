import { Scope } from '@/lib/auth/scopes';

export interface NotificationTab {
  href: string;
  label: string;
  /** The scope the page needs. A tab the session cannot use is not drawn. */
  scope: string;
}

/**
 * The pages under "แจ้งเตือนลูกค้า", in the order they are drawn. Add a page by adding a line
 * here; the tab bar and the
 * `/notifications` redirect both read this list.
 */
export const notificationTabs: NotificationTab[] = [
  { href: '/notifications/log', label: 'ประวัติการส่ง', scope: Scope.NotifyRead },
  { href: '/notifications/send', label: 'ส่งข้อความ', scope: Scope.NotifySend },
  { href: '/notifications/broadcast', label: 'ประกาศทาง LINE', scope: Scope.NotifyBroadcast },
  { href: '/notifications/templates', label: 'เทมเพลต', scope: Scope.NotifyManage },
  { href: '/notifications/automations', label: 'เตือนอัตโนมัติ', scope: Scope.AutomationRead },
  { href: '/notifications/reminders', label: 'ข้อความที่จะส่ง', scope: Scope.AutomationRead },
];

export const notificationsArea = {
  tabsLabel: 'หมวดการแจ้งเตือน',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ใช้หน้าการแจ้งเตือน',
};
