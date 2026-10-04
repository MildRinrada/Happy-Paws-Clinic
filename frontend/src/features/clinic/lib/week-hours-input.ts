import type { OpeningHours, Weekday } from '../types/clinic.types';
import type { DayHoursInput } from '../types/settings.types';
import { WEEKDAY_LABELS } from './weekdays';

const WEEK = Object.keys(WEEKDAY_LABELS) as Weekday[];

/** What a clinic that has never set its hours starts from: every day 09:00 to 18:00. */
const DEFAULT_DAY = { opens: '09:00', closes: '18:00', closed: false };

/** The week as the form edits it, Monday first, with every day present. */
export function weekHoursInput(hours: OpeningHours): DayHoursInput[] {
  return WEEK.map((day) => {
    const stored = hours.configured ? hours.days.find((entry) => entry.day === day) : undefined;

    return stored
      ? { day, opens: stored.opens ?? DEFAULT_DAY.opens, closes: stored.closes ?? DEFAULT_DAY.closes, closed: stored.closed }
      : { day, ...DEFAULT_DAY };
  });
}

/** The week as `PUT /clinic/hours` takes it: a closed day carries no times. */
export function weekHoursPayload(days: DayHoursInput[]): DayHoursInput[] {
  return days.map(({ day, opens, closes, closed }) => (closed ? { day, closed } : { day, opens, closes, closed }));
}

/**
 * A refusal's per-day messages. The backend names them `days[2].closes`; the form shows them
 * under the day they are about.
 */
export function dayErrors(errors: Record<string, string>): Record<number, string> {
  const byDay: Record<number, string> = {};

  for (const [field, message] of Object.entries(errors)) {
    const match = /^days\[(\d+)\]/.exec(field);
    if (match) byDay[Number(match[1])] ??= message;
  }

  return byDay;
}
