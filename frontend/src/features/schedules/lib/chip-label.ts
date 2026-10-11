import { dayChipParts } from '@/features/booking/lib/booking-dates';
import { schedulePage } from '../content/schedule-page';
import type { Shift } from '../types/schedule.types';
import { clinicClock } from './clinic-clock';
import type { ShiftChip } from './shift-chips';

/** "09:00-12:00" for one block. */
export function blockHours(block: Pick<Shift, 'starts_at' | 'ends_at'>): string {
  return `${clinicClock(block.starts_at)}-${clinicClock(block.ends_at)}`;
}

/** "09:00-18:00 พัก 12:00-13:00": the whole shift, with each break named. */
export function chipLabel(chip: ShiftChip): string {
  const hours = `${clinicClock(chip.startsAt)}-${clinicClock(chip.endsAt)}`;
  const breaks = chip.breaks.map((gap) => schedulePage.breakNote(clinicClock(gap.startsAt), clinicClock(gap.endsAt)));

  return [hours, ...breaks].join(' ');
}

/** "จ. 12 ต.ค." from the fixed tables, safe to render on the server and in the browser alike. */
export function dayLabel(day: string): string {
  const { weekday, date } = dayChipParts(day);

  return `${weekday} ${date}`;
}
