import { staffApi } from '@/lib/api/server-client';
import type { AdjustmentInput, StockChange } from '../types/inventory.types';

/**
 * `POST /staff/inventory/adjustments`: "ปรับยอดหลังนับสต๊อก, ต้องระบุเหตุผล". Needs
 * `inventory:adjust`. Sets the lot to what was counted; the difference is worked out by the backend.
 */
export function adjustStock(input: AdjustmentInput): Promise<StockChange> {
  return staffApi().post<StockChange>('/staff/inventory/adjustments', { body: input });
}
