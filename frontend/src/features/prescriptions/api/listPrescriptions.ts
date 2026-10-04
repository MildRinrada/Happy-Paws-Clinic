import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { PrescriptionItem } from '../types/prescription.types';

export interface ListPrescriptionsQuery {
  /** One patient's whole history, across branches. Without it, the caller's branches only. */
  petId?: string;
  /** `false` is what the pharmacy has yet to hand over. */
  dispensed?: boolean;
  cursor?: string;
  limit?: number;
}

/**
 * `GET /staff/prescriptions?pet_id=`: "ยาที่เคยจ่ายทั้งหมด, ตรวจซ้ำซ้อน", newest first. Needs
 * `rx:read`. With `pet_id` the read is written to the access log. `dispensed` is in the backend
 * but not in the contract's line (docs/features/clinical-records.md).
 */
export function listPrescriptions(query: ListPrescriptionsQuery): Promise<CursorPage<PrescriptionItem>> {
  return staffApi().get<CursorPage<PrescriptionItem>>('/staff/prescriptions', {
    query: { pet_id: query.petId, dispensed: query.dispensed, cursor: query.cursor, limit: query.limit ?? 50 },
  });
}
