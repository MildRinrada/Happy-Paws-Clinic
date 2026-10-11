import { staffApi } from '@/lib/api/server-client';
import type { Branch, ClinicProfile, Holiday, OpeningHours } from '../types/clinic.types';
import type {
  BrandImageKind,
  BrandImageType,
  BrandImageUpload,
  BranchInput,
  BranchUpdateInput,
  ClinicProfileInput,
  DayHoursInput,
  HolidayInput,
} from '../types/settings.types';

/**
 * The writes behind the public clinic pages, all on `settings:write`. Their reads are public and
 * live beside them (`getClinicProfile`, `listBranches`, `getOpeningHours`).
 */

/** `PATCH /clinic` — profile and branding. */
export function updateClinicProfile(version: number, input: ClinicProfileInput): Promise<ClinicProfile> {
  return staffApi().patch<ClinicProfile>('/clinic', { body: input, ifMatch: version });
}

/**
 * `POST /clinic/logo` or `POST /clinic/tab-icon` (docs/contract-extensions.md § 50): a signed URL
 * for the file and the value that names it. The profile does not change until it is saved.
 */
export function requestBrandImage(
  kind: BrandImageKind,
  input: { content_type: BrandImageType; size_bytes: number },
): Promise<BrandImageUpload> {
  return staffApi().post<BrandImageUpload>(`/clinic/${kind}`, { body: input });
}

/** `POST /clinic/branches` — a new site. */
export function createBranch(input: BranchInput): Promise<Branch> {
  return staffApi().post<Branch>('/clinic/branches', { body: input });
}

/**
 * `PATCH /clinic/branches/{id}` (docs/contract-extensions.md § 51) — name, address, pin and exam
 * rooms. The code never changes. A room a ticket on the queue is still in cannot be taken away.
 */
export function updateBranch(id: string, version: number, input: BranchUpdateInput): Promise<Branch> {
  return staffApi().patch<Branch>(`/clinic/branches/${encodeURIComponent(id)}`, { body: input, ifMatch: version });
}

/** `PUT /clinic/hours` — the whole week, seven days, each once. */
export function replaceOpeningHours(version: number, days: DayHoursInput[]): Promise<OpeningHours> {
  return staffApi().put<OpeningHours>('/clinic/hours', { body: { days }, ifMatch: version });
}

/** `POST /clinic/holidays` — closes booking on that day. There is no removal in the contract. */
export function addHoliday(input: HolidayInput): Promise<Holiday> {
  return staffApi().post<Holiday>('/clinic/holidays', { body: input });
}
