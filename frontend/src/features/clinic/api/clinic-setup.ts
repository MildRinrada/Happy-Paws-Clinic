import { staffApi } from '@/lib/api/server-client';
import type { Branch, ClinicProfile, Holiday, OpeningHours } from '../types/clinic.types';
import type { BranchInput, ClinicProfileInput, DayHoursInput, HolidayInput } from '../types/settings.types';

/**
 * The writes behind the public clinic pages, all on `settings:write`. Their reads are public and
 * live beside them (`getClinicProfile`, `listBranches`, `getOpeningHours`).
 */

/** `PATCH /clinic` — profile and branding. */
export function updateClinicProfile(version: number, input: ClinicProfileInput): Promise<ClinicProfile> {
  return staffApi().patch<ClinicProfile>('/clinic', { body: input, ifMatch: version });
}

/** `POST /clinic/branches` — a new site. There is no edit: the contract opens branches only. */
export function createBranch(input: BranchInput): Promise<Branch> {
  return staffApi().post<Branch>('/clinic/branches', { body: input });
}

/** `PUT /clinic/hours` — the whole week, seven days, each once. */
export function replaceOpeningHours(version: number, days: DayHoursInput[]): Promise<OpeningHours> {
  return staffApi().put<OpeningHours>('/clinic/hours', { body: { days }, ifMatch: version });
}

/** `POST /clinic/holidays` — closes booking on that day. There is no removal in the contract. */
export function addHoliday(input: HolidayInput): Promise<Holiday> {
  return staffApi().post<Holiday>('/clinic/holidays', { body: input });
}
