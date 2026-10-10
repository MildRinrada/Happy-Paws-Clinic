/**
 * Dates on the review pages, in clinic time. A visit's day comes as `YYYY-MM-DD` with no time, so
 * it is read as midday in Bangkok, which no time zone can push onto another day.
 */

const day = new Intl.DateTimeFormat('th-TH', {
  timeZone: 'Asia/Bangkok',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
});

const shortDay = new Intl.DateTimeFormat('th-TH', {
  timeZone: 'Asia/Bangkok',
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

/** "8 ตุลาคม 2569" for a `YYYY-MM-DD` day. */
export function formatVisitDay(value: string): string {
  return day.format(new Date(`${value}T12:00:00+07:00`));
}

/** "8 ต.ค. 2569" for an instant, the date a review was written. */
export function formatReviewDate(iso: string): string {
  return shortDay.format(new Date(iso));
}
