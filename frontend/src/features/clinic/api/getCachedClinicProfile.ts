import { publicApi } from '@/lib/api/server-client';
import type { ClinicProfile } from '../types/clinic.types';

/** The tag a profile save drops, so the new logo shows on the next request instead of minutes later. */
export const CLINIC_PROFILE_TAG = 'clinic-profile';

/** Seconds every visitor shares one read of the profile for the logo and the tab icon. */
const KEEP_SECONDS = 300;

/**
 * `GET /clinic` for the frame of every page (the logo, the name beside it, the tab icon), shared by
 * every visitor for five minutes. The settings page reads it fresh with `getClinicProfile` instead.
 */
export function getCachedClinicProfile(): Promise<ClinicProfile> {
  return publicApi().get<ClinicProfile>('/clinic', { revalidate: KEEP_SECONDS, tags: [CLINIC_PROFILE_TAG] });
}
