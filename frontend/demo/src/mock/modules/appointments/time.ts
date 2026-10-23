/**
 * The clinic's wall clock. Appointments, shifts and the queue all live on Bangkok time (UTC+7 all
 * year, no daylight saving), whatever the visitor's laptop is set to, as the product reads them.
 */

const OFFSET_MINUTES = 7 * 60;

const pad = (value: number) => String(value).padStart(2, '0');

/** The clinic day (`YYYY-MM-DD`) an instant falls on. */
export function clinicDayOf(instant: string | number | Date = new Date()): string {
  const shifted = new Date(new Date(instant).getTime() + OFFSET_MINUTES * 60_000);
  return `${shifted.getUTCFullYear()}-${pad(shifted.getUTCMonth() + 1)}-${pad(shifted.getUTCDate())}`;
}

export const clinicToday = () => clinicDayOf(new Date());

/** `day` plus `days`, as a clinic day. */
export function addDays(day: string, days: number): string {
  const date = new Date(`${day}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

/** A clinic day `days` from today. */
export const clinicDay = (days = 0) => addDays(clinicToday(), days);

/** The instant a clinic day reads `HH:mm`, written with the clinic's offset as the backend writes it. */
export function instant(day: string, time: string): string {
  return new Date(`${day}T${time.length === 5 ? `${time}:00` : time}+07:00`).toISOString();
}

/** Minutes after midnight on the clinic's wall clock. */
export function clinicMinutes(value: string | number | Date): number {
  const date = new Date(value);
  return (date.getUTCHours() * 60 + date.getUTCMinutes() + OFFSET_MINUTES) % (24 * 60);
}

/** "08:30" to 510. */
export function minutesOf(time: string): number {
  const [hours = 0, minutes = 0] = time.split(':').map(Number);
  return hours * 60 + minutes;
}

/** 510 to "08:30". */
export function timeOf(minutes: number): string {
  return `${pad(Math.floor(minutes / 60))}:${pad(minutes % 60)}`;
}

const WEEKDAYS = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'] as const;
export type Weekday = (typeof WEEKDAYS)[number];

export function weekdayOf(day: string): Weekday {
  return WEEKDAYS[new Date(`${day}T00:00:00Z`).getUTCDay()]!;
}

/** "14:30" as the clinic reads an instant. */
export function clockOf(value: string): string {
  return timeOf(clinicMinutes(value));
}

const THAI_MONTHS = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.'];

/** "24 ต.ค. 69 เวลา 14:30", for the messages the clinic sends. */
export function thaiWhen(value: string): string {
  const day = clinicDayOf(value);
  const [year, month, date] = day.split('-').map(Number);
  return `${date} ${THAI_MONTHS[(month ?? 1) - 1]} ${String((year ?? 2026) + 543).slice(-2)} เวลา ${clockOf(value)} น.`;
}

export function overlaps(aStart: string, aEnd: string, bStart: string, bEnd: string): boolean {
  return aStart < bEnd && bStart < aEnd;
}

export function addMinutes(value: string, minutes: number): string {
  return new Date(new Date(value).getTime() + minutes * 60_000).toISOString();
}
