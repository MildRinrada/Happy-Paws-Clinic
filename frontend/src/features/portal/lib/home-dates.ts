import { clinicDay, formatDateTime, formatTime } from '@/lib/format/datetime';
import { portalHome } from '../content/portal-home';

/** Dates on the portal's home page, in clinic time. */

const shortDay = new Intl.DateTimeFormat('th-TH', {
  timeZone: 'Asia/Bangkok',
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

/** "3 ต.ค. 2569" for a `YYYY-MM-DD` day, read as midday in Bangkok so no time zone moves it. */
export function formatDueDay(value: string): string {
  return shortDay.format(new Date(`${value}T12:00:00+07:00`));
}

/**
 * When a message came: "วันนี้ 08:12", "เมื่อวาน 16:40", and the date with the time before that.
 * Worked out on the server for each request, so it is never stale by more than one page load.
 */
export function formatSentAt(iso: string, now: Date = new Date()): string {
  const day = clinicDay(iso);
  const time = formatTime(iso);

  if (day === clinicDay(now)) return portalHome.notifications.today(time);
  if (day === clinicDay(new Date(now.getTime() - 86_400_000))) return portalHome.notifications.yesterday(time);

  return formatDateTime(iso);
}
