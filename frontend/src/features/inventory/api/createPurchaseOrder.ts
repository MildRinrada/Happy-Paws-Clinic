import { staffApi } from '@/lib/api/server-client';
import type { PurchaseOrder, PurchaseOrderInput } from '../types/inventory.types';

/**
 * `POST /staff/inventory/purchase-orders`: "สั่งซื้อจากซัพพลายเออร์, รับของบางส่วนได้". Needs
 * `inventory:purchase`. Changes no stock: the goods arrive later as receipts naming the order.
 */
export function createPurchaseOrder(input: PurchaseOrderInput, idempotencyKey: string): Promise<PurchaseOrder> {
  return staffApi().post<PurchaseOrder>('/staff/inventory/purchase-orders', { body: input, idempotencyKey });
}
