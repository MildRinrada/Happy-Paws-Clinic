import type { Weekday } from './clinic.types';

/** `PATCH /clinic` — every field sent; an empty string clears an optional one. */
export interface ClinicProfileInput {
  name: string;
  logo_url: string;
  brand_color: string;
  license_no: string;
  operator_name: string;
}

/** `POST /clinic/branches`. The code becomes the id: `sukhumvit39` is `brn_sukhumvit39`. */
export interface BranchInput {
  code: string;
  name: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  exam_rooms: string[];
}

/** One day of `PUT /clinic/hours`. A closed day sends no times. */
export interface DayHoursInput {
  day: Weekday;
  opens?: string;
  closes?: string;
  closed: boolean;
}

/** `POST /clinic/holidays`. Without `branch_id` the whole clinic closes. */
export interface HolidayInput {
  date: string;
  name: string;
  branch_id?: string;
}

/** `GET` / `PATCH /clinic/settings/booking`. */
export interface BookingSettings {
  version: number;
  slot_minutes: number;
  lead_time_minutes: number;
  cancellation_cutoff_hours: number;
  max_advance_days: number;
  /** Stored and shown, but nothing enforces it yet. */
  walk_in_slots_per_hour: number;
}

export type BookingSettingsInput = Omit<BookingSettings, 'version'>;

/** `PATCH /clinic/settings/queue`. `example` is the 14th ticket of a day under these settings. */
export interface QueueSettings {
  version: number;
  number_prefix: string;
  number_digits: number;
  reset_daily: boolean;
  example: string;
}

export type QueueSettingsInput = Omit<QueueSettings, 'version' | 'example'>;

/** `PATCH /clinic/settings/medical-record`. */
export interface MedicalRecordSettings {
  version: number;
  required_fields: string[];
  examination_systems: string[];
  owner_visible_sections: string[];
  signer_must_be_case_vet: boolean;
}

export type MedicalRecordSettingsInput = Omit<MedicalRecordSettings, 'version'>;
