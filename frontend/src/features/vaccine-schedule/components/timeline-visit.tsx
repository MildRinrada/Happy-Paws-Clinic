import { vaccinePage } from '../content/vaccine-schedule-page';
import { ageText } from '../lib/age-text';
import { formatThaiDay } from '../lib/calendar-date';
import type { PlannedVisit, VisitStatus } from '../types/vaccine-schedule.types';
import { VisitStatusBadge } from './visit-status-badge';

const dots: Record<VisitStatus, string> = {
  done: 'bg-ink-300',
  overdue: 'bg-site-alert',
  due: 'bg-site-orange',
  upcoming: 'bg-site-teal',
};

/** One visit on the timeline: when, how old the pet will be, which vaccines, and any note. */
export function TimelineVisit({ visit, birthDate }: { visit: PlannedVisit; birthDate: string }) {
  const text = vaccinePage.result;
  const isCatchUp = visit.missedSince !== undefined;

  return (
    <li className="relative break-inside-avoid pb-6 pl-8 last:pb-0">
      {/* The dot on the line, coloured by status. The line itself is the list's left border. */}
      <span aria-hidden="true" className={`absolute left-[-7px] top-1.5 size-3.5 rounded-full ring-4 ring-white ${dots[visit.status]}`} />

      <div className="flex flex-wrap items-center gap-x-3 gap-y-1.5">
        <p className="text-lg font-bold leading-tight">
          {isCatchUp ? text.missedSince.replace('{date}', formatThaiDay(visit.missedSince!)) : formatThaiDay(visit.date)}
        </p>
        <VisitStatusBadge status={visit.status} />
      </div>
      {!isCatchUp && (
        <p className="mt-0.5 text-[13px] text-site-muted">{text.ageAt.replace('{age}', ageText(birthDate, visit.date))}</p>
      )}

      <h3 className="mt-2 text-base font-semibold">{visit.title}</h3>
      <ul className="mt-1.5 space-y-1 text-[15px]">
        {visit.vaccines.map((vaccine) => (
          <li key={vaccine.id}>
            <span className="font-semibold text-site-teal">{vaccine.name}</span>{' '}
            <span className="text-ink-700">
              {text.protects}
              {vaccine.protects}
            </span>
          </li>
        ))}
      </ul>
      {visit.note && <p className="mt-2 text-[13px] leading-relaxed text-site-muted">{visit.note}</p>}
    </li>
  );
}
