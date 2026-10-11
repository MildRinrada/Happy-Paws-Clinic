import { clinicDay } from '@/lib/format/datetime';

/**
 * Dates on the pet pages, in clinic time, written from fixed tables rather than `Intl`: the
 * server's and the browser's ICU data disagree on some Thai short names (see
 * `features/booking/lib/booking-dates.ts`), and these words must read the same wherever they are drawn.
 */

const MONTHS = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.'];

/** Buddhist Era, as Thai dates are written. */
const BE_OFFSET = 543;

/** Year, month (1 to 12) and day of a `YYYY-MM-DD` day as it is, or of an instant on the clinic's calendar. */
function partsOf(value: string): [number, number, number] {
  const day = /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : clinicDay(value);
  const [year = 0, month = 1, date = 1] = day.split('-').map(Number);

  return [year, month, date];
}

/** "12 ต.ค. 2569". */
export function thaiDate(value: string): string {
  const [year, month, day] = partsOf(value);
  return `${day} ${MONTHS[month - 1] ?? ''} ${year + BE_OFFSET}`;
}

/** "ต.ค. 69", under a bar of the weight chart. */
export function thaiMonthYear(value: string): string {
  const [year, month] = partsOf(value);
  return `${MONTHS[month - 1] ?? ''} ${String((year + BE_OFFSET) % 100).padStart(2, '0')}`;
}

/** "12 ต.ค.", when two weighings fall in one month and the month alone would repeat. */
export function thaiDayMonth(value: string): string {
  const [, month, day] = partsOf(value);
  return `${day} ${MONTHS[month - 1] ?? ''}`;
}
