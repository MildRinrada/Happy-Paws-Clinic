import { Scope } from '@/lib/auth/scopes';

export interface InventoryTab {
  href: string;
  label: string;
  /** The scope the page needs. A tab the session cannot use is not drawn. */
  scope: string;
  /** The list page is the area's root, so it is current only on its own path. */
  exact?: boolean;
}

/** The pages under "สต๊อกยา", in the order they are drawn. The tab bar and `/inventory` both read this. */
export const inventoryTabs: InventoryTab[] = [
  { href: '/inventory', label: 'รายการยา', scope: Scope.InventoryRead, exact: true },
  { href: '/inventory/alerts', label: 'แจ้งเตือน', scope: Scope.InventoryRead },
  { href: '/inventory/movements', label: 'รับเข้า จ่ายออก', scope: Scope.InventoryWrite },
  { href: '/inventory/adjustments', label: 'ปรับยอด', scope: Scope.InventoryAdjust },
  { href: '/inventory/purchase-orders', label: 'สั่งซื้อ', scope: Scope.InventoryPurchase },
];

export const inventoryArea = {
  tabsLabel: 'หมวดสต๊อกยา',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ใช้หน้าสต๊อกยา',
  noBranch: 'บัญชีนี้ไม่ได้สังกัดสาขาที่เปิดอยู่ จึงไม่มีสต๊อกให้ดู',
  branchNote: (name: string) => `สาขา ${name}`,
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  conflict: 'ข้อมูลถูกแก้ไประหว่างบันทึก โหลดหน้าใหม่แล้วลองอีกครั้ง',
};
