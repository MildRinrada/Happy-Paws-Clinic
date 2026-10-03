import type { TeamMember } from '../types/team.types';

/** "อายุรกรรมสัตว์เล็ก | 8 ปี", or whichever half is known. Empty when neither is. */
export function memberSummary(member: Pick<TeamMember, 'specialty' | 'years_of_experience'>): string {
  const years = member.years_of_experience;
  // A no-break space, so "ปี" never wraps away from its number.
  const experience = years === undefined ? undefined : years < 1 ? 'น้อยกว่า 1 ปี' : `${years} ปี`;

  return [member.specialty, experience].filter(Boolean).join(' | ');
}

/** Where a person's card sits on the team page. */
export function memberHref(id: string): string {
  return `/team#${id}`;
}

/** Where a department starts on the team page. */
export function departmentHref(department: string): string {
  return `/team#${department}`;
}
