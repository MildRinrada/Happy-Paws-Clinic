import type {
  PetSchedule,
  PlannedVisit,
  ScheduleRules,
  SpeciesPlan,
  VaccineInfo,
  VisitStatus,
  VisitTemplate,
} from '../types/vaccine-schedule.types';
import { addDays, addMonths, laterOf } from './calendar-date';

interface BuildInput {
  birthDate: string;
  lastVaccineDate?: string;
  /** Today in Bangkok, `YYYY-MM-DD`. Passed in so the result depends on nothing but its inputs. */
  today: string;
}

interface CatchUpText {
  title: string;
  note: string;
}

// A 30-year-old cat has some 35 visits behind it; this only stops a bad input looping forever.
const MAX_VISITS = 80;

const templateAt = (plan: SpeciesPlan, index: number): VisitTemplate => plan.visits[index] ?? plan.repeat;

/** When a visit falls, given the birth date and the date of the visit before it. */
function dateFor(template: VisitTemplate, birthDate: string, previous: string | undefined, rules: ScheduleRules): string {
  const { timing } = template;
  const planned =
    timing.kind === 'age-weeks'
      ? addDays(birthDate, timing.weeks * 7)
      : timing.kind === 'age-months'
        ? addMonths(birthDate, timing.months)
        : addMonths(previous ?? birthDate, timing.months);

  // Never closer to the visit before than the series allows.
  return previous ? laterOf(planned, addDays(previous, rules.minGapDays)) : planned;
}

function vaccinesOf(plan: SpeciesPlan, ids: string[]): VaccineInfo[] {
  return ids.map((id) => plan.vaccines.find((vaccine) => vaccine.id === id)).filter((v): v is VaccineInfo => Boolean(v));
}

function toVisit(plan: SpeciesPlan, template: VisitTemplate, index: number, date: string, status: VisitStatus): PlannedVisit {
  return {
    key: `${template.id}-${index}`,
    title: template.title,
    date,
    status,
    vaccines: vaccinesOf(plan, template.vaccineIds),
    note: template.note,
  };
}

/** Several missed visits become one: come in now, and the vet works out the catch-up. */
function catchUp(missed: [PlannedVisit, ...PlannedVisit[]], today: string, text: CatchUpText): PlannedVisit {
  const [first] = missed;

  if (missed.length === 1) {
    const note = [first.note, text.note].filter(Boolean).join(' ');
    return { ...first, note, missedSince: first.date, missedCount: 1 };
  }

  const vaccines = new Map<string, VaccineInfo>();
  missed.forEach((visit) => visit.vaccines.forEach((vaccine) => vaccines.set(vaccine.id, vaccine)));

  return {
    key: 'catch-up',
    title: text.title,
    date: today,
    status: 'overdue',
    vaccines: [...vaccines.values()],
    note: text.note,
    missedSince: first.date,
    missedCount: missed.length,
  };
}

/**
 * A pet's own timeline from the general schedule, its birth date and, when known, the day it last
 * had a vaccine.
 *
 * - Visits falling on or before the last vaccine date are taken as done, and the next one is
 *   counted from that date (a yearly booster a year after it, a series dose at least
 *   `minGapDays` after it).
 * - Visits before today that are not done are overdue. They become one catch-up visit, and what
 *   follows is counted from today, as if the pet came in now.
 * - The timeline runs `horizonMonths` past today, and always shows at least one visit ahead.
 *
 * Pure: the same inputs give the same timeline, on the server or in the browser.
 */
export function buildSchedule(
  plan: SpeciesPlan,
  { birthDate, lastVaccineDate, today }: BuildInput,
  rules: ScheduleRules,
  catchUpText: CatchUpText,
): PetSchedule {
  const done: PlannedVisit[] = [];
  const missed: PlannedVisit[] = [];
  const upcoming: PlannedVisit[] = [];
  const horizon = addMonths(today, rules.horizonMonths);
  const dueSoon = addDays(today, rules.dueSoonDays);

  let previous: string | undefined;
  let lastApplied = !lastVaccineDate;
  let caughtUp = false;

  for (let index = 0; index < MAX_VISITS; index++) {
    const template = templateAt(plan, index);
    let date = dateFor(template, birthDate, previous, rules);

    if (!lastApplied && lastVaccineDate) {
      if (date <= lastVaccineDate) {
        done.push(toVisit(plan, template, index, date, 'done'));
        previous = date;
        continue;
      }

      // The first visit after the last vaccine counts from the day that vaccine was given.
      lastApplied = true;
      previous = lastVaccineDate;
      date = dateFor(template, birthDate, previous, rules);
    }

    if (date < today) {
      missed.push(toVisit(plan, template, index, date, 'overdue'));
      previous = date;
      continue;
    }

    if (missed.length > 0 && !caughtUp) {
      caughtUp = true;
      previous = today;
      date = dateFor(template, birthDate, previous, rules);
    }

    if (date > horizon && upcoming.length > 0) {
      break;
    }

    upcoming.push(toVisit(plan, template, index, date, date <= dueSoon ? 'due' : 'upcoming'));
    previous = date;
  }

  const [firstMissed, ...otherMissed] = missed;

  return {
    done,
    ahead: firstMissed ? [catchUp([firstMissed, ...otherMissed], today, catchUpText), ...upcoming] : upcoming,
  };
}
