import { addDays, dayChipParts } from '@/features/booking/lib/booking-dates';
import { WEEKDAY_LABELS } from '@/features/clinic/lib/weekdays';
import { weekdayOf } from '@/features/emergency/lib/clinic-clock';
import { clinicDay, formatTime } from '@/lib/format/datetime';
import { waitEstimate } from '../content/wait-estimate';
import type { WaitEstimate } from '../types/wait-estimate.types';

/** "วันนี้", "พรุ่งนี้", or "วันจันทร์ที่ 12 ต.ค." for the day a closed branch opens again. */
function openingDay(opensAt: string, now: Date): string {
  const day = clinicDay(opensAt);
  const today = clinicDay(now);

  if (day === today) return waitEstimate.today;
  if (day === addDays(today, 1)) return waitEstimate.tomorrow;

  return waitEstimate.laterDay(WEEKDAY_LABELS[weekdayOf(day)], dayChipParts(day).date);
}

/** The sentence for one branch: the wait, no wait, or closed (and when it opens, when known). */
export function waitLine(estimate: WaitEstimate, now: Date): { text: string; detail?: string } {
  const text = waitEstimate;

  if (!estimate.open_now) {
    return {
      text: text.closed,
      detail: estimate.opens_at ? text.opensAgain(openingDay(estimate.opens_at, now), formatTime(estimate.opens_at)) : undefined,
    };
  }

  if (estimate.waiting === 0 || estimate.estimate_minutes_low === undefined || estimate.estimate_minutes_high === undefined) {
    return { text: text.noWait };
  }

  return { text: text.waiting(estimate.estimate_minutes_low, estimate.estimate_minutes_high, estimate.waiting) };
}
