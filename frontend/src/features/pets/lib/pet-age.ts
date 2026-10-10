import { clinicDay } from '@/lib/format/datetime';

/** Whole years and the months left over. */
export interface PetAge {
  years: number;
  months: number;
}

/**
 * How old a pet born on `birthDate` (`YYYY-MM-DD`) is on the clinic's `today`, counted in whole
 * months the way a vet says it: "3 ปี 2 เดือน". Undefined for a date it cannot read.
 */
export function petAge(birthDate: string, today: string = clinicDay(new Date())): PetAge | undefined {
  const parts = [...birthDate.split('-'), ...today.split('-')].map(Number);
  if (parts.length !== 6 || !parts.every(Number.isFinite)) return undefined;
  const [bornYear = 0, bornMonth = 0, bornDay = 0, year = 0, month = 0, day = 0] = parts;

  const months = (year - bornYear) * 12 + (month - bornMonth) - (day < bornDay ? 1 : 0);
  if (months < 0) return undefined;

  return { years: Math.floor(months / 12), months: months % 12 };
}

const birthDay = new Intl.DateTimeFormat('th-TH', { timeZone: 'UTC', day: 'numeric', month: 'long', year: 'numeric' });

/** A birth date is a calendar day, not an instant, so it is read in UTC and never shifts a day. */
export function formatBirthDate(birthDate: string): string {
  return birthDay.format(new Date(`${birthDate}T00:00:00Z`));
}
