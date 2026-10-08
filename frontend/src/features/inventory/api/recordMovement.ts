import { staffApi } from '@/lib/api/server-client';
import type { MovementInput, StockChange } from '../types/inventory.types';

/**
 * `POST /staff/inventory/movements`: "รับเข้า จ่ายออก โอนสาขา, อ้างอิงเอกสารต้นทาง". Needs
 * `inventory:write`. The same key on a retry answers with the receipt already recorded instead of
 * putting the delivery on the shelf twice.
 */
export function recordMovement(input: MovementInput, idempotencyKey: string): Promise<StockChange> {
  return staffApi().post<StockChange>('/staff/inventory/movements', { body: input, idempotencyKey });
}
