import type { StaffMember, UpdateStaffInput } from '../types/staff.types';

export interface MemberDraft {
  staff_type: string;
  department: string;
  license_no: string;
  roles: string[];
  branches: string[];
}

export const draftOf = (member: StaffMember): MemberDraft => ({
  staff_type: member.staff_type,
  department: member.department,
  license_no: member.license_no ?? '',
  roles: member.roles,
  branches: member.branches,
});

const sameSet = (a: string[], b: string[]) => a.length === b.length && a.every((value) => b.includes(value));

/**
 * Only what changed goes into the PATCH. A field left out is left alone, and each one sent is
 * checked again: resending unchanged roles would re-run the grant check, and resending branches
 * would drop any the member has that this caller cannot see.
 */
export function memberChanges(member: StaffMember, draft: MemberDraft): UpdateStaffInput {
  const before = draftOf(member);
  const changes: UpdateStaffInput = {};

  if (draft.staff_type.trim() !== before.staff_type) changes.staff_type = draft.staff_type.trim();
  if (draft.department !== before.department) changes.department = draft.department;
  // An empty string is how the API clears a licence number.
  if (draft.license_no.trim() !== before.license_no) changes.license_no = draft.license_no.trim();
  if (!sameSet(draft.roles, before.roles)) changes.roles = draft.roles;
  if (!sameSet(draft.branches, before.branches)) changes.branches = draft.branches;

  return changes;
}
