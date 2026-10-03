import { getOpeningHours } from '@/features/clinic/api/getOpeningHours';
import { listBranches } from '@/features/clinic/api/listBranches';
import type { Branch, OpeningHours, Weekday } from '@/features/clinic/types/clinic.types';

export interface VisitInfo {
  branches: Branch[];
  hours: OpeningHours;
}

/**
 * The branches and opening hours for the about page, or `null` if the API cannot be reached.
 * Like the team page: the rest of the page still renders, and says so where these would be.
 */
export async function loadVisitInfo(): Promise<VisitInfo | null> {
  try {
    const [branches, hours] = await Promise.all([listBranches(), getOpeningHours()]);
    return { branches, hours };
  } catch {
    return null;
  }
}

const clinicWeekday = new Intl.DateTimeFormat('en-US', { timeZone: 'Asia/Bangkok', weekday: 'long' });

/** Today's weekday in clinic time, to mark its row. */
export function clinicToday(now: Date = new Date()): Weekday {
  return clinicWeekday.format(now).toLowerCase() as Weekday;
}
