import { cache } from 'react';
import { optional } from '@/lib/api/api-error';
import { authed } from '@/lib/auth/session';
import { getPetRecords } from '../api/getPetRecords';
import type { OwnerRecords } from '../types/records.types';

/**
 * `GET /portal/pets/{id}/records`, once per request, for a pet whose link lets this owner see its
 * history. A co-carer without that right is not asked at all: the answer would only be a `404`.
 * Takes plain values, so React's per-request cache matches two callers asking about one pet.
 */
export const loadPetRecords = cache(
  async (petId: string, mayViewHistory: boolean): Promise<OwnerRecords | null> =>
    mayViewHistory ? authed(() => optional(getPetRecords(petId))) : null,
);
