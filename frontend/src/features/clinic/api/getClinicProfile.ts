import { publicApi } from '@/lib/api/server-client';
import type { ClinicProfile } from '../types/clinic.types';

/** `GET /clinic` — public. An instance nobody has configured still answers, with its default name. */
export function getClinicProfile(): Promise<ClinicProfile> {
  return publicApi().get<ClinicProfile>('/clinic');
}
