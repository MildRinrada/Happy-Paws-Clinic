import type { BadgeTone } from '@/components/ui/badge';
import type { PurchaseOrderStatus } from '../types/inventory.types';

/** The order list under the purchase-order form (docs/contract-extensions.md § 25). */
export const purchaseOrderListText = {
  heading: 'ใบสั่งซื้อของสาขา',
  hint: 'ใหม่สุดก่อน ยกเลิกได้เฉพาะใบที่ยังมีของค้างรับ ของที่รับแล้วยังอยู่ในคลัง',
  empty: 'ยังไม่มีใบสั่งซื้อตามตัวกรองนี้',
  filters: {
    label: 'กรองใบสั่งซื้อ',
    supplier: 'ซัพพลายเออร์',
    status: 'สถานะ',
    any: 'ทั้งหมด',
    apply: 'กรอง',
  },
  orderedAt: (when: string) => `สั่งเมื่อ ${when}`,
  line: (name: string, received: number, quantity: number, unit: string) => `${name}: รับแล้ว ${received} จาก ${quantity} ${unit}`,
  cancelledLine: (count: number) => `ยกเลิก ${count}`,
  total: (amount: string) => `รวม ${amount}`,
  receivedCost: (amount: string) => `รับแล้ว ${amount}`,
  cancelledNote: (reason?: string) => (reason ? `ยกเลิกแล้ว: ${reason}` : 'ยกเลิกแล้ว'),
  next: 'หน้าถัดไป',
  first: 'กลับหน้าแรก',
  cancel: {
    button: 'ยกเลิกส่วนที่ค้าง',
    confirm: 'ของที่ยังไม่มาจะถูกตัดออกจากใบนี้และรับตามใบนี้ไม่ได้อีก ของที่รับแล้วยังอยู่ในคลัง',
    confirmButton: 'ยกเลิกใบสั่งซื้อ',
    pending: 'กำลังยกเลิก…',
    back: 'ไม่ยกเลิก',
  },
};

export const purchaseOrderStatusLabels: Record<PurchaseOrderStatus, { text: string; tone: BadgeTone }> = {
  ordered: { text: 'สั่งแล้ว', tone: 'blue' },
  partially_received: { text: 'รับบางส่วน', tone: 'amber' },
  received: { text: 'รับครบ', tone: 'green' },
  cancelled: { text: 'ยกเลิกแล้ว', tone: 'neutral' },
};
