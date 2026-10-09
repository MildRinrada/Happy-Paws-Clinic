import { Scope } from '@/lib/auth/scopes';

export interface CrmTab {
  href: string;
  label: string;
  /** The scope the page needs. A tab the session cannot use is not drawn. */
  scope: string;
}

/**
 * The pages under "แพ็กเกจและโปรโมชัน", in the order they are drawn. The tab bar and the `/crm`
 * redirect both read this list.
 */
export const crmTabs: CrmTab[] = [
  { href: '/crm/packages', label: 'ขายแพ็กเกจ', scope: Scope.CrmManage },
  { href: '/crm/coupons', label: 'สร้างคูปอง', scope: Scope.CrmManage },
  { href: '/crm/segments', label: 'กลุ่มลูกค้า', scope: Scope.CrmManage },
  { href: '/crm/loyalty', label: 'แต้มและสมาชิก', scope: Scope.CrmRead },
];

/** Words every CRM page shares. */
export const crmArea = {
  tabsLabel: 'หมวดแพ็กเกจและโปรโมชัน',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ใช้หน้าแพ็กเกจและโปรโมชัน',
  noManage: 'บัญชีนี้ไม่มีสิทธิ์จัดการแพ็กเกจและโปรโมชัน (crm:manage)',
  noBranch: 'ยังไม่มีสาขาที่บัญชีนี้เข้าถึงได้',
  branchNote: (name: string) => `สาขา ${name}`,
  conflict: 'ข้อมูลถูกแก้ไประหว่างบันทึก โหลดหน้าใหม่แล้วลองอีกครั้ง',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  metaTitle: (page: string) => `${page} · Happy Paws Clinic`,
};
