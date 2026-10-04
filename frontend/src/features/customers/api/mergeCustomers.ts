import { staffApi } from '@/lib/api/server-client';
import type { MergeResult } from '../types/customer.types';

/**
 * `POST /staff/customers/{id}/merge` — "รวมเรกคอร์ดซ้ำ · ย้ายสัตว์และประวัติทั้งหมด". Needs
 * `customer:merge`. **The path id is the record that survives**; the body names the duplicate it
 * absorbs. Not reversible through the API. `version` is the survivor's, in `If-Match`.
 *
 * Refusals: `409 state_conflict` when either record is already merged or both hold a portal
 * login, `409 version_conflict` when the survivor changed since the page loaded.
 */
export function mergeCustomers(survivingId: string, version: number, duplicateId: string): Promise<MergeResult> {
  return staffApi().post<MergeResult>(`/staff/customers/${encodeURIComponent(survivingId)}/merge`, {
    ifMatch: version,
    body: { duplicate_customer_id: duplicateId },
  });
}
