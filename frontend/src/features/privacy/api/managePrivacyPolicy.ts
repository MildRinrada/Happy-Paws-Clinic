import { staffApi } from '@/lib/api/server-client';
import type { PolicySection } from '../types/privacy-policy.types';
import type { DraftSource, PublishInput, StaffPolicyList, StaffPolicyVersion } from '../types/staff-privacy-policy.types';

/**
 * The console's side of the privacy notice (docs/contract-extensions.md § 35). Reading needs
 * `settings:read`, drafting `settings:write`, publishing `settings:danger` (the owner).
 */

const base = '/staff/privacy-policy/versions';

/** `GET /staff/privacy-policy/versions`: every version with its text, and the template's news. */
export function listPolicyVersions(): Promise<StaffPolicyList> {
  return staffApi().get<StaffPolicyList>(base);
}

/** `POST /staff/privacy-policy/versions`: a new draft from the template, the current notice, or a version. */
export function createPolicyDraft(source: DraftSource): Promise<StaffPolicyVersion> {
  return staffApi().post<StaffPolicyVersion>(base, { body: source });
}

/** `PATCH /staff/privacy-policy/versions/{id}`: every section, in order. `If-Match` is required. */
export function updatePolicyDraft(id: string, sections: PolicySection[], version: number): Promise<StaffPolicyVersion> {
  return staffApi().patch<StaffPolicyVersion>(`${base}/${encodeURIComponent(id)}`, { body: { sections }, ifMatch: version });
}

/** `POST /staff/privacy-policy/versions/{id}/publish`: the clinic's notice from now. `If-Match` is required. */
export function publishPolicy(id: string, input: PublishInput, version: number): Promise<StaffPolicyVersion> {
  return staffApi().post<StaffPolicyVersion>(`${base}/${encodeURIComponent(id)}/publish`, { body: input, ifMatch: version });
}
