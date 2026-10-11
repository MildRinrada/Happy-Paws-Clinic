import { addDays, dayChipParts } from '@/features/booking/lib/booking-dates';
import type { CopyWeekPlan, ScheduleCalendar } from '../types/schedule.types';
import { shiftChips } from './shift-chips';

const weekLabel = (monday: string) => `${dayChipParts(monday).date} - ${dayChipParts(addDays(monday, 6)).date}`;

/**
 * What "คัดลอกเวรสัปดาห์ก่อน" would copy onto the week in view: the week before's shifts at this
 * branch, a shift with a break counted once, and how many people they belong to. An unread week
 * before counts as empty; the backend decides what is really there when the copy is asked for.
 */
export function copyPlan(lastWeek: ScheduleCalendar | null, days: string[]): CopyWeekPlan {
  const toWeek = days[0]!;
  const fromWeek = addDays(toWeek, -7);
  const lastDays = Array.from({ length: 7 }, (_, index) => addDays(fromWeek, index));
  const counts = (lastWeek?.staff ?? []).map((person) =>
    lastDays.reduce((total, day) => total + shiftChips(person.shifts, day).length, 0),
  );

  return {
    fromWeek,
    toWeek,
    fromLabel: weekLabel(fromWeek),
    toLabel: weekLabel(toWeek),
    shifts: counts.reduce((total, count) => total + count, 0),
    people: counts.filter((count) => count > 0).length,
  };
}
