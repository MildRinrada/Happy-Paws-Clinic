import type { Ref } from 'react';
import { SiteCard } from '@/components/site/site-card';
import { vaccinePlans } from '../content/vaccine-plans';
import { vaccinePage } from '../content/vaccine-schedule-page';
import { ageText } from '../lib/age-text';
import { formatThaiDay } from '../lib/calendar-date';
import type { PetSchedule, ScheduleActions as Actions, ScheduleInput } from '../types/vaccine-schedule.types';
import { DoneVisits } from './done-visits';
import { MedicalDisclaimer } from './medical-disclaimer';
import { NextVisitBanner } from './next-visit-banner';
import { ScheduleActions } from './schedule-actions';
import { TimelineVisit } from './timeline-visit';

interface Props {
  input: ScheduleInput;
  today: string;
  schedule: PetSchedule;
  actions: Actions;
  /** Focused when a new timeline appears, so the reader lands on it. */
  headingRef: Ref<HTMLHeadingElement>;
}

/** The pet's own timeline, the warning that goes with it, then what to do next. */
export function ScheduleResult({ input, today, schedule, actions, headingRef }: Props) {
  const text = vaccinePage.result;
  const pet = vaccinePlans[input.species].label;
  const [next] = schedule.ahead;

  return (
    <div className="space-y-6">
      <section aria-labelledby="vaccine-result-heading" className="space-y-5">
        <p className="hidden text-sm print:block">
          {text.printTitle} | {text.printedOn.replace('{date}', formatThaiDay(today))}
        </p>

        <div>
          <h2
            id="vaccine-result-heading"
            ref={headingRef}
            tabIndex={-1}
            className="text-2xl font-semibold leading-tight outline-none sm:text-[30px]"
          >
            {text.heading.replace('{pet}', pet)}
          </h2>
          <p className="mt-1.5 text-[15px] text-site-muted">
            {text.born.replace('{date}', formatThaiDay(input.birthDate)).replace('{age}', ageText(input.birthDate, today))}
          </p>
        </div>

        {next && <NextVisitBanner visit={next} today={today} />}

        <SiteCard padding="lg" className="print:border-0 print:p-0">
          <h3 className="text-lg font-bold">{text.aheadHeading}</h3>
          <ol className="ml-2 mt-5 border-l-2 border-ink-100">
            {schedule.ahead.map((visit) => (
              <TimelineVisit key={visit.key} visit={visit} birthDate={input.birthDate} />
            ))}
          </ol>
        </SiteCard>

        {schedule.done.length > 0 && <DoneVisits visits={schedule.done} birthDate={input.birthDate} />}

        <MedicalDisclaimer />
      </section>

      <ScheduleActions actions={actions} species={input.species} />
    </div>
  );
}
