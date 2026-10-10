import { clinicDay } from '@/lib/format/datetime';
import { getAvailability } from '../api/getAvailability';
import { guestBookingPage } from '../content/guest-booking-page';
import type { BookingDetails, TimePreselect } from '../types/booking.types';
import { addDays, clinicInstant } from './booking-dates';

const DAY = /^\d{4}-\d{2}-\d{2}$/;
const TIME = /^([01]\d|2[0-3]):[0-5]\d$/;

/** True for a real calendar day written `YYYY-MM-DD`, not one like `2026-02-31`. */
function isRealDay(day: string): boolean {
  return DAY.test(day) && addDays(day, 0) === day;
}

/**
 * The time a link to `/book` asks for (`?date=2026-10-10&time=14:30`, as the home page's "ว่างเร็วสุด"
 * button sends), checked on the server before the form draws.
 *
 * Used only when the address also settled on a service and a branch the clinic offers (`initial`,
 * from `preselect`), the day and time are well formed, and the time is still ahead. Then the day's
 * free times are read once (`GET /booking/availability`, a read: nothing is held):
 *
 * - the time is still free: the form opens on the phone step with it picked, as if the visitor had
 *   tapped it in the grid. Sending the phone holds it, and the backend still refuses a time taken
 *   in the meantime, which sends the visitor back to the times as usual.
 * - it is no longer free: the form opens on that day's times with a short note.
 * - the read fails: the form opens on that day's times, which load and report on their own.
 *
 * Anything else answers `undefined`, and the form opens on its first step as without these.
 */
export async function preselectTime(
  initial: BookingDetails,
  date?: string,
  time?: string,
  now: Date = new Date(),
): Promise<TimePreselect | undefined> {
  if (!date || !time || !initial.serviceId || !initial.branchId) return undefined;
  if (!isRealDay(date) || !TIME.test(time)) return undefined;

  const wanted = clinicInstant(date, time);
  if (date < clinicDay(now) || Date.parse(wanted) <= now.getTime()) return undefined;

  try {
    const { slots } = await getAvailability(initial.serviceId, date, initial.branchId);
    const free = slots.find((slot) => Date.parse(slot.starts_at) === Date.parse(wanted));

    return free ? { day: date, startsAt: free.starts_at } : { day: date, notice: guestBookingPage.timeTaken(time) };
  } catch {
    return { day: date };
  }
}
