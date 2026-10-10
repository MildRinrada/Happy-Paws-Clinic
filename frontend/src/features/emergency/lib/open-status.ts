import { addDays } from '@/features/booking/lib/booking-dates';
import type { Branch, OpeningHours } from '@/features/clinic/types/clinic.types';
import type { BranchOpenStatus, OpenState } from '../types/emergency.types';
import { clinicClock, weekdayOf } from './clinic-clock';

/** How many days ahead to look for the next opening. */
const LOOK_AHEAD = 14;

/**
 * Whether one branch is open at `now`, from the clinic's weekly hours and the closures still to
 * come (`GET /clinic/hours`). A closure without a branch shuts every branch; one with a branch
 * shuts only that one. Hours are the same at every branch, as the clinic sets them once.
 *
 * Times are "HH:mm" on the clinic's clock and compare as text. The backend keeps every day's
 * closing time after its opening time, so a day never runs past midnight.
 */
export function openState(hours: OpeningHours, branchId: string | undefined, now: Date): { state: OpenState; holiday?: string } {
  const { day: today, time } = clinicClock(now);
  const closure = (day: string) => hours.holidays.find((item) => item.date === day && (!item.branch_id || item.branch_id === branchId));
  const hoursOn = (day: string) => {
    const week = hours.days.find((item) => item.day === weekdayOf(day));
    return week && !week.closed && week.opens && week.closes ? { opens: week.opens, closes: week.closes } : undefined;
  };

  const holiday = closure(today)?.name;

  for (let offset = 0; offset < LOOK_AHEAD; offset++) {
    const day = addDays(today, offset);
    const open = closure(day) ? undefined : hoursOn(day);

    if (!open) continue;

    if (offset === 0) {
      if (time < open.opens) return { state: { kind: 'opens-later', day, offset, opens: open.opens }, holiday };
      if (time < open.closes) return { state: { kind: 'open', closes: open.closes }, holiday };
      continue;
    }

    return { state: { kind: 'opens-later', day, offset, opens: open.opens }, holiday };
  }

  return { state: { kind: 'closed' }, holiday };
}

/**
 * Open or closed at each branch, in the order the branches are listed. A clinic with one branch,
 * or whose branches could not be read, gets one answer with no branch name.
 */
export function branchStatuses(hours: OpeningHours, branches: Branch[], now: Date): BranchOpenStatus[] {
  if (branches.length <= 1) {
    return [{ key: branches[0]?.id ?? 'clinic', ...openState(hours, branches[0]?.id, now) }];
  }

  return branches.map((branch) => ({ key: branch.id, branchName: branch.name, ...openState(hours, branch.id, now) }));
}
