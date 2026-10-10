import { vaccinePage } from '../content/vaccine-schedule-page';
import type { PlannedVisit } from '../types/vaccine-schedule.types';
import { TimelineVisit } from './timeline-visit';

/**
 * The visits taken as done from the last vaccine date, folded away: they are an assumption from
 * one date, worth checking against the vaccine book, not the point of the page.
 */
export function DoneVisits({ visits, birthDate }: { visits: PlannedVisit[]; birthDate: string }) {
  const text = vaccinePage.result;

  return (
    <details className="rounded-2xl border border-ink-200 bg-white p-4 sm:p-5 print:hidden">
      <summary className="cursor-pointer text-[15px] font-semibold">{text.doneSummary.replace('{n}', String(visits.length))}</summary>
      <p className="mt-2 text-[13px] text-site-muted">{text.doneNote}</p>
      <ol className="ml-2 mt-4 border-l-2 border-ink-100">
        {visits.map((visit) => (
          <TimelineVisit key={visit.key} visit={visit} birthDate={birthDate} />
        ))}
      </ol>
    </details>
  );
}
