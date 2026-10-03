/**
 * Every time on screen is clinic time. The API sends instants with an offset; the clinic reads
 * them as the wall clock in Bangkok reads them, whatever the viewer's laptop is set to.
 */

const CLINIC_TIME_ZONE = 'Asia/Bangkok';

const time = new Intl.DateTimeFormat('th-TH', {
  timeZone: CLINIC_TIME_ZONE,
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

const dateTime = new Intl.DateTimeFormat('th-TH', {
  timeZone: CLINIC_TIME_ZONE,
  day: 'numeric',
  month: 'short',
  year: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

const longDate = new Intl.DateTimeFormat('th-TH', {
  timeZone: CLINIC_TIME_ZONE,
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
});

// en-CA writes dates as YYYY-MM-DD, which compares as a plain string.
const dayKey = new Intl.DateTimeFormat('en-CA', {
  timeZone: CLINIC_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

/** The clinic's calendar day an instant falls on, as `YYYY-MM-DD`. Two instants on one clinic day match. */
export function clinicDay(value: Date | string): string {
  return dayKey.format(typeof value === 'string' ? new Date(value) : value);
}

export function formatTime(iso: string): string {
  return time.format(new Date(iso));
}

export function formatDateTime(iso: string): string {
  return dateTime.format(new Date(iso));
}

export function formatLongDate(value: Date | string): string {
  return longDate.format(typeof value === 'string' ? new Date(value) : value);
}

/** Whole minutes from an instant until now. Never negative: a clock a few seconds off is not news. */
export function minutesSince(iso: string, now: Date = new Date()): number {
  return Math.max(0, Math.floor((now.getTime() - Date.parse(iso)) / 60_000));
}
