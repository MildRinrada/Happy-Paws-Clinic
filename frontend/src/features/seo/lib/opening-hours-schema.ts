import type { DayHours, OpeningHours } from '@/features/clinic/types/clinic.types';
import type { JsonLdObject } from './json-ld';

const schemaDay = (day: DayHours['day']) => day.charAt(0).toUpperCase() + day.slice(1);

/**
 * The weekly hours as schema.org `OpeningHoursSpecification`, one entry per distinct pair of
 * opening and closing times, so "Monday to Friday 09:00 to 20:00" is one entry. Closed days are
 * left out, which is how schema.org says a day is closed.
 */
export function weeklyHoursSchema(hours: OpeningHours): JsonLdObject[] {
  const groups = new Map<string, { opens: string; closes: string; days: string[] }>();

  for (const day of hours.days) {
    if (day.closed || !day.opens || !day.closes) {
      continue;
    }

    const key = `${day.opens}-${day.closes}`;
    const group = groups.get(key) ?? { opens: day.opens, closes: day.closes, days: [] };
    group.days.push(schemaDay(day.day));
    groups.set(key, group);
  }

  return [...groups.values()].map((group) => ({
    '@type': 'OpeningHoursSpecification',
    dayOfWeek: group.days,
    opens: group.opens,
    closes: group.closes,
  }));
}

/**
 * The clinic-wide special closures still to come. schema.org marks a closed day as opening and
 * closing at 00:00. A branch's own closure is not the whole clinic's, so it is left out.
 */
export function closuresSchema(hours: OpeningHours): JsonLdObject[] {
  return hours.holidays
    .filter((holiday) => !holiday.branch_id)
    .map((holiday) => ({
      '@type': 'OpeningHoursSpecification',
      validFrom: holiday.date,
      validThrough: holiday.date,
      opens: '00:00',
      closes: '00:00',
    }));
}
