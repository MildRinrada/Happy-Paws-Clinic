import type { Shift } from '../types/schedule.types';
import type { RosterRow } from './roster-rows';
import { touchesDay } from './roster-week';

/** Who is on, per day: the two counts the summary row shows. */
export interface DayCover {
  vets: number;
  frontDesk: number;
}

const isOn = (row: RosterRow, day: string) => row.shifts.some((shift) => touchesDay(shift.starts_at, shift.ends_at, day));

/** For each day of the week, how many vets and how many front-desk staff have a shift that day. */
export function dayCover(rows: RosterRow[], days: string[]): Record<string, DayCover> {
  return Object.fromEntries(
    days.map((day) => [
      day,
      {
        vets: rows.filter((row) => row.group === 'vets' && isOn(row, day)).length,
        frontDesk: rows.filter((row) => row.group === 'frontDesk' && isOn(row, day)).length,
      },
    ]),
  );
}

/** Hours worked in the blocks given. A break is already a gap between blocks, so it is never counted. */
export function workedHours(shifts: Shift[]): number {
  const ms = shifts.reduce((total, shift) => total + (Date.parse(shift.ends_at) - Date.parse(shift.starts_at)), 0);

  return Math.round((ms / 3_600_000) * 10) / 10;
}
