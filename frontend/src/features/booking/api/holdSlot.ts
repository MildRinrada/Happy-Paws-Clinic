import { staffApi } from '@/lib/api/server-client';
import type { SlotHold } from '../types/booking.types';

export interface HoldSlotInput {
  serviceId: string;
  branchId: string;
  startsAt: string;
  /** Left out for "any vet": the backend then picks one who is free. */
  staffId?: string;
}

/**
 * `POST /booking/holds` — keeps the time for ten minutes. There is no way to release a hold early;
 * one that is not used simply lapses.
 */
export function holdSlot({ serviceId, branchId, startsAt, staffId }: HoldSlotInput): Promise<SlotHold> {
  return staffApi().post<SlotHold>('/booking/holds', {
    body: { service_id: serviceId, branch_id: branchId, starts_at: startsAt, staff_id: staffId },
  });
}
