/**
 * The roster's row groups, read from each person's department code (docs/contract-extensions.md
 * § 10), never from names or roles: vets, the front desk, assistants, and everyone else.
 */
export type RosterGroupKey = 'vets' | 'frontDesk' | 'assistants' | 'others';

/** The order the groups are shown in. */
export const ROSTER_GROUPS: RosterGroupKey[] = ['vets', 'frontDesk', 'assistants', 'others'];

const BY_DEPARTMENT: Record<string, RosterGroupKey> = {
  veterinary: 'vets',
  front_desk: 'frontDesk',
  nursing: 'assistants',
};

export function groupOf(department: string | undefined): RosterGroupKey {
  return (department && BY_DEPARTMENT[department]) || 'others';
}
