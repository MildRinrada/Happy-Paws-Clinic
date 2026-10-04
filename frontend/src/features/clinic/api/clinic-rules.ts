import { staffApi } from '@/lib/api/server-client';
import type {
  BookingSettings,
  BookingSettingsInput,
  MedicalRecordSettings,
  MedicalRecordSettingsInput,
  QueueSettings,
  QueueSettingsInput,
} from '../types/settings.types';

/**
 * The clinic's operating rules. Only booking has a `GET` in the contract; queue and medical-record
 * settings are `PATCH`-only, and an empty `PATCH` body is how they are read (it writes nothing and
 * returns them as they stand, docs/assumptions.md § 27).
 */

/** `GET /clinic/settings/booking` — `settings:read`. */
export function getBookingSettings(): Promise<BookingSettings> {
  return staffApi().get<BookingSettings>('/clinic/settings/booking');
}

/** `PATCH /clinic/settings/booking` — `settings:write`. */
export function updateBookingSettings(version: number, input: BookingSettingsInput): Promise<BookingSettings> {
  return staffApi().patch<BookingSettings>('/clinic/settings/booking', { body: input, ifMatch: version });
}

/** The queue settings as they stand: an empty `PATCH`, which needs `settings:write`. */
export function readQueueSettings(): Promise<QueueSettings> {
  return staffApi().patch<QueueSettings>('/clinic/settings/queue', { body: {} });
}

export function updateQueueSettings(version: number, input: QueueSettingsInput): Promise<QueueSettings> {
  return staffApi().patch<QueueSettings>('/clinic/settings/queue', { body: input, ifMatch: version });
}

/** The medical-record settings as they stand: an empty `PATCH`. */
export function readMedicalRecordSettings(): Promise<MedicalRecordSettings> {
  return staffApi().patch<MedicalRecordSettings>('/clinic/settings/medical-record', { body: {} });
}

export function updateMedicalRecordSettings(
  version: number,
  input: MedicalRecordSettingsInput,
): Promise<MedicalRecordSettings> {
  return staffApi().patch<MedicalRecordSettings>('/clinic/settings/medical-record', { body: input, ifMatch: version });
}
