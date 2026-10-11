import type { StaffSchedule } from '../types/schedule.types';
import { ROSTER_GROUPS, groupOf, type RosterGroupKey } from './department-groups';

/** One person's row on the roster, with or without anything planned this week. */
export interface RosterRow extends StaffSchedule {
  group: RosterGroupKey;
  /** The signed-in person, whose row leads their group. */
  isMe: boolean;
}

export interface RosterGroup {
  key: RosterGroupKey;
  rows: RosterRow[];
}

/** A staff list entry, as much of it as the roster needs. */
export interface RosterMember {
  id: string;
  display_name: string;
  department: string;
}

/**
 * Everyone the roster should show: each person with a shift or leave this week, and, when the
 * staff list could be read, every active member of the branch, so someone with nothing planned
 * shows as an empty row to plan into.
 */
export function rosterRows(calendar: StaffSchedule[], members: RosterMember[] | null, meId: string): RosterRow[] {
  const byId = new Map<string, StaffSchedule>(calendar.map((person) => [person.staff_id, person]));

  for (const member of members ?? []) {
    if (!byId.has(member.id)) {
      byId.set(member.id, {
        staff_id: member.id,
        display_name: member.display_name,
        department: member.department,
        shifts: [],
        time_off: [],
      });
    }
  }

  return [...byId.values()].map((person) => ({
    ...person,
    group: groupOf(person.department),
    isMe: person.staff_id === meId,
  }));
}

/** The rows in their department groups, in the groups' order; the signed-in person first in theirs, then by name. */
export function groupRows(rows: RosterRow[]): RosterGroup[] {
  return ROSTER_GROUPS.map((key) => ({
    key,
    rows: rows
      .filter((row) => row.group === key)
      .sort((a, b) => Number(b.isMe) - Number(a.isMe) || a.display_name.localeCompare(b.display_name, 'th')),
  })).filter((group) => group.rows.length > 0);
}
