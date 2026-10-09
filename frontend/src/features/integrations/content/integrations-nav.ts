import { Scope } from '@/lib/auth/scopes';

export interface IntegrationsNavItem {
  href: string;
  label: string;
  /** The scope the page needs; `null` for a page every staff member may open. */
  scope: string | null;
}

/** The "เชื่อมต่อระบบ" pages. The calendar link is everyone's; the rest are `integration:manage`. */
export const integrationsNav: IntegrationsNavItem[] = [
  { href: '/settings/integrations/webhooks', label: 'Webhook', scope: Scope.IntegrationManage },
  { href: '/settings/integrations/api-keys', label: 'API key', scope: Scope.IntegrationManage },
  { href: '/settings/integrations/payment', label: 'ผู้ให้บริการชำระเงิน', scope: Scope.IntegrationManage },
  { href: '/settings/integrations/lab', label: 'แล็บภายนอก', scope: Scope.IntegrationManage },
  { href: '/settings/integrations/calendar', label: 'ปฏิทิน', scope: null },
];

export const integrationsArea = {
  navLabel: 'หมวดการเชื่อมต่อระบบ',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ตั้งค่าการเชื่อมต่อระบบ',
};
