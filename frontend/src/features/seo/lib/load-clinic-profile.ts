import { cache } from 'react';
import { getClinicProfile } from '@/features/clinic/api/getClinicProfile';
import type { ClinicProfile } from '@/features/clinic/types/clinic.types';

/**
 * `GET /clinic` once per request for structured data, or `null` when the API is away. Structured
 * data is extra: a page must render without it.
 */
export const loadClinicProfile = cache(async (): Promise<ClinicProfile | null> => {
  try {
    return await getClinicProfile();
  } catch {
    return null;
  }
});
