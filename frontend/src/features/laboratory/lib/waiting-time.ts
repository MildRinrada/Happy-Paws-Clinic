import { labPage } from '../content/lab-page';
import { labTiming } from '../content/lab-timing';
import type { LabRequest } from '../types/lab.types';

export interface LabWait {
  minutes: number;
  /** "รอ 25 นาที", "รอ 1 วัน 2 ชม.". */
  text: string;
  /** Still waiting on the lab for longer than the clinic allows for where the sample went. */
  overdue: boolean;
}

/** Whole minutes as the row says them: minutes under an hour, hours and minutes under a day, then days and hours. */
export function waitText(minutes: number): string {
  const text = labPage.waiting;
  if (minutes < 60) return text.minutes(minutes);
  if (minutes < 24 * 60) return text.hours(Math.floor(minutes / 60), minutes % 60);

  return text.days(Math.floor(minutes / (24 * 60)), Math.floor((minutes % (24 * 60)) / 60));
}

/**
 * How long a request has been waiting, and on whom: a pending one since it was sent (waiting on the
 * lab), a resulted one since its latest results (waiting on a vet). A read request waits on nobody.
 *
 * `now` comes from the server render, so the text the server sends is the text the browser keeps:
 * the row is never computed twice against two clocks.
 */
export function labWait(request: LabRequest, now: Date): LabWait | undefined {
  const since = request.status === 'pending' ? request.requested_at : request.status === 'resulted' ? request.resulted_at : undefined;
  if (!since) return undefined;

  const minutes = Math.max(0, Math.floor((now.getTime() - Date.parse(since)) / 60_000));
  const limit = request.destination === 'external' ? labTiming.externalOverdueMinutes : labTiming.inHouseOverdueMinutes;

  return { minutes, text: waitText(minutes), overdue: request.status === 'pending' && minutes > limit };
}
