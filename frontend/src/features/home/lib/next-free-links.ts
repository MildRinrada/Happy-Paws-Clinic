import { clinicDay, formatTime } from '@/lib/format/datetime';

const BOOK_PATH = '/book';

/**
 * The booking page with the service, branch and time picked: `/book?service=&branch=&date=&time=`.
 * The page checks the time is still free before it opens on it.
 */
export function bookTimeHref(serviceId: string, branchId: string, startsAt: string): string {
  const query = new URLSearchParams({ service: serviceId, branch: branchId, date: clinicDay(startsAt), time: formatTime(startsAt) });
  return `${BOOK_PATH}?${query}`;
}

/** The booking page with only the service picked, for "ดูเวลาว่างทั้งหมด". */
export function bookServiceHref(serviceId: string): string {
  return `${BOOK_PATH}?${new URLSearchParams({ service: serviceId })}`;
}
