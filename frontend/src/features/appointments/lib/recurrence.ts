import { addDays, clinicInstant } from '@/features/booking/lib/booking-dates';
import { clinicDay } from '@/lib/format/datetime';

/**
 * A course's repeat rule as the form edits it, and the dates it gives. The backend expands the
 * RRULE itself (`ShiftRecurrence`); this mirrors the part the form can write, so the preview shows
 * the same dates the backend will try to book: every N days, or every N weeks on the first visit's
 * weekday, at the first visit's clinic time.
 */

export type RepeatFrequency = 'daily' | 'weekly';
export type RepeatEnd = 'count' | 'until';

export interface RepeatRule {
  frequency: RepeatFrequency;
  interval: string;
  end: RepeatEnd;
  count: string;
  /** `YYYY-MM-DD`, the last day a visit may fall on. */
  until: string;
}

export type RepeatProblem = 'tooFew' | 'tooMany' | 'tooFar' | 'badInterval' | 'badUntil';

/** `SeriesBooker.MaxVisits` and the RRULE horizon (`ShiftRecurrence.MaxHorizon`). */
export const MIN_VISITS = 2;
export const MAX_VISITS = 52;
const HORIZON_DAYS = 366;

/** The contract's own example, "ทุก 3 สัปดาห์ 6 ครั้ง". */
export function defaultRepeat(): RepeatRule {
  return { frequency: 'weekly', interval: '3', end: 'count', count: '6', until: '' };
}

function wholeNumber(text: string): number | null {
  return /^\d{1,4}$/.test(text.trim()) ? Number(text.trim()) : null;
}

/** The RRULE the backend reads, e.g. `FREQ=WEEKLY;INTERVAL=3;COUNT=6`. */
export function toRrule(rule: RepeatRule): string {
  const parts = [`FREQ=${rule.frequency === 'daily' ? 'DAILY' : 'WEEKLY'}`, `INTERVAL=${wholeNumber(rule.interval) ?? 1}`];
  parts.push(rule.end === 'count' ? `COUNT=${wholeNumber(rule.count) ?? 0}` : `UNTIL=${rule.until.replaceAll('-', '')}`);
  return parts.join(';');
}

export interface Expansion {
  /** Each visit's start, as the instant the API expects. Stops one past the limit. */
  starts: string[];
  problem?: RepeatProblem;
}

/** The visits a rule gives from a first start, and what is wrong with it, if anything. */
export function expand(firstStartsAt: string, rule: RepeatRule): Expansion {
  const interval = wholeNumber(rule.interval);
  if (!interval || interval < 1) return { starts: [], problem: 'badInterval' };

  const firstDay = clinicDay(firstStartsAt);
  const time = clinicTime(firstStartsAt);
  const step = rule.frequency === 'daily' ? interval : interval * 7;
  const count = rule.end === 'count' ? wholeNumber(rule.count) : null;
  const until = rule.end === 'until' ? rule.until : null;

  if (rule.end === 'until' && (!until || until < firstDay)) return { starts: [], problem: 'badUntil' };
  if (rule.end === 'count' && count === null) return { starts: [], problem: 'tooFew' };

  const days: string[] = [];
  for (let day = firstDay; days.length <= MAX_VISITS; day = addDays(day, step)) {
    if (count !== null && days.length >= count) break;
    if (until !== null && day > until) break;
    days.push(day);
  }

  const starts = days.map((day) => clinicInstant(day, time));
  const last = days.at(-1);

  if (days.length < MIN_VISITS) return { starts, problem: 'tooFew' };
  if (days.length > MAX_VISITS) return { starts, problem: 'tooMany' };
  if (last && last > addDays(firstDay, HORIZON_DAYS)) return { starts, problem: 'tooFar' };
  return { starts };
}

/** "HH:mm" on the clinic's wall clock. Bangkok keeps +07:00 all year. */
export function clinicTime(iso: string): string {
  return new Date(Date.parse(iso) + 7 * 3_600_000).toISOString().slice(11, 16);
}
