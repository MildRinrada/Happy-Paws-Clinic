import { Scope } from '@/lib/auth/scopes';
import type { NavBadgeKey, StaffIconName } from '../types/staff-shell.types';

export interface StaffNavItem {
  href: string;
  label: string;
  icon: StaffIconName;
  /** The scopes the screens behind the link are built on; holding any one shows the link. */
  scopes: string[];
  /** Shown only when the staff AI assistant is switched on for this deployment (§ 34). */
  ai?: boolean;
  /** The count of waiting work drawn beside the label, when there is one. */
  badge?: NavBadgeKey;
}

export interface StaffNavGroup {
  key: string;
  label: string;
  items: StaffNavItem[];
}

/**
 * The console's menu, grouped the way a day at the clinic runs: what is happening now, the care
 * itself, money and stock, keeping in touch with owners, and running the clinic. A group whose
 * entries this session cannot use is not drawn at all.
 */
export const staffNavigation: StaffNavGroup[] = [
  {
    key: 'today',
    label: 'งานวันนี้',
    items: [
      { href: '/dashboard', label: 'ภาพรวม', icon: 'dashboard', scopes: [Scope.DashboardRead] },
      { href: '/queue', label: 'คิววันนี้', icon: 'queue', scopes: [Scope.QueueRead], badge: 'queue' },
      { href: '/appointments', label: 'ปฏิทินนัด', icon: 'calendar', scopes: [Scope.AppointmentRead] },
      { href: '/messages', label: 'ข้อความลูกค้า', icon: 'messages', scopes: [Scope.MessageRead], badge: 'messages' },
    ],
  },
  {
    key: 'care',
    label: 'ดูแลรักษา',
    items: [
      { href: '/customers', label: 'ลูกค้าและสัตว์', icon: 'customers', scopes: [Scope.CustomerRead] },
      { href: '/lab', label: 'แล็บ', icon: 'lab', scopes: [Scope.LabRead, Scope.LabWrite], badge: 'lab' },
      { href: '/prescriptions', label: 'จ่ายยา', icon: 'prescriptions', scopes: [Scope.PrescriptionDispense] },
    ],
  },
  {
    key: 'money',
    label: 'การเงินและคลัง',
    items: [
      { href: '/billing', label: 'เก็บเงิน', icon: 'billing', scopes: [Scope.InvoiceWrite] },
      { href: '/inventory', label: 'สต๊อกยา', icon: 'inventory', scopes: [Scope.InventoryRead], badge: 'stock' },
      { href: '/crm', label: 'แพ็กเกจและโปรโมชัน', icon: 'crm', scopes: [Scope.CrmRead, Scope.CrmManage] },
    ],
  },
  {
    key: 'relations',
    label: 'ลูกค้าสัมพันธ์',
    items: [
      {
        href: '/notifications',
        label: 'แจ้งเตือนลูกค้า',
        icon: 'notifications',
        scopes: [Scope.NotifyRead, Scope.NotifySend, Scope.AutomationRead],
      },
    ],
  },
  {
    key: 'clinic',
    label: 'จัดการคลินิก',
    items: [
      { href: '/schedules', label: 'ตารางเวร', icon: 'schedules', scopes: [Scope.ScheduleRead] },
      { href: '/reports', label: 'รายงาน', icon: 'reports', scopes: [Scope.StatsRead, Scope.AnalyticsRead, Scope.AnalyticsHr] },
      { href: '/ai', label: 'ผู้ช่วย AI', icon: 'ai', scopes: [Scope.AiUse], ai: true },
      {
        href: '/settings',
        label: 'ตั้งค่า',
        icon: 'settings',
        scopes: [Scope.SettingsWrite, Scope.ServiceManage, Scope.StaffRead, Scope.ContentManage],
      },
    ],
  },
];
