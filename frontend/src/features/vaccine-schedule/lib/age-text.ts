import { vaccinePage } from '../content/vaccine-schedule-page';
import { daysBetween, monthsBetween } from './calendar-date';

const text = vaccinePage.age;

/** How old a pet born on `birthDate` is on `day`: weeks while young, then months, then years. */
export function ageText(birthDate: string, day: string): string {
  const days = daysBetween(birthDate, day);

  if (days < 7) {
    return text.newborn;
  }
  if (days < 7 * 20) {
    return text.weeks.replace('{n}', String(Math.floor(days / 7)));
  }

  const months = monthsBetween(birthDate, day);

  if (months < 12) {
    return text.months.replace('{n}', String(months));
  }

  const years = Math.floor(months / 12);
  const rest = months % 12;

  return rest === 0
    ? text.years.replace('{n}', String(years))
    : text.yearsMonths.replace('{y}', String(years)).replace('{m}', String(rest));
}
