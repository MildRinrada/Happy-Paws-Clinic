import type { Weekday } from '@/features/clinic/types/clinic.types';

const CODES: Record<Weekday, string> = {
  monday: 'MO',
  tuesday: 'TU',
  wednesday: 'WE',
  thursday: 'TH',
  friday: 'FR',
  saturday: 'SA',
  sunday: 'SU',
};

/**
 * "These weekdays until that day" as the RRULE the backend reads:
 * `FREQ=WEEKLY;BYDAY=MO,WE,FR;UNTIL=20261231`. The days keep the order they were given.
 */
export function weeklyRule(days: Weekday[], until: string): string {
  return `FREQ=WEEKLY;BYDAY=${days.map((day) => CODES[day]).join(',')};UNTIL=${until.replaceAll('-', '')}`;
}
