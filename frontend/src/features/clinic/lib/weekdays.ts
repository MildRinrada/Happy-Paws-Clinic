import type { DayHours, Weekday } from '../types/clinic.types';

/** Monday first, as a Thai calendar on a clinic wall reads. */
export const WEEKDAY_LABELS: Record<Weekday, string> = {
  monday: 'จันทร์',
  tuesday: 'อังคาร',
  wednesday: 'พุธ',
  thursday: 'พฤหัสบดี',
  friday: 'ศุกร์',
  saturday: 'เสาร์',
  sunday: 'อาทิตย์',
};

const WEEK = Object.keys(WEEKDAY_LABELS) as Weekday[];

/** The week Monday first, whatever order the API sent it in. */
export function inWeekOrder(days: DayHours[]): DayHours[] {
  return [...days].sort((a, b) => WEEK.indexOf(a.day) - WEEK.indexOf(b.day));
}

export interface HoursRow {
  /** "จันทร์", or a run like "จันทร์ - ศุกร์". */
  days: string;
  /** "08:00 - 20:00", or `closedText`. */
  hours: string;
  closed: boolean;
}

/**
 * The week folded into runs of days with the same hours, for a footer or a one-line summary:
 * seven identical days become one row, a closed Wednesday splits the run.
 */
export function hoursRows(days: DayHours[], closedText: string): HoursRow[] {
  const rows: (HoursRow & { from: Weekday; to: Weekday })[] = [];

  for (const day of inWeekOrder(days)) {
    const closed = day.closed || !day.opens || !day.closes;
    const hours = closed ? closedText : `${day.opens} - ${day.closes}`;
    const last = rows.at(-1);

    if (last && last.hours === hours && WEEK.indexOf(day.day) === WEEK.indexOf(last.to) + 1) {
      last.to = day.day;
    } else {
      rows.push({ from: day.day, to: day.day, days: '', hours, closed });
    }
  }

  return rows.map(({ from, to, hours, closed }) => ({
    days: from === to ? WEEKDAY_LABELS[from] : `${WEEKDAY_LABELS[from]} - ${WEEKDAY_LABELS[to]}`,
    hours,
    closed,
  }));
}
