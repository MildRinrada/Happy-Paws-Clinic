import { SiteCard } from '@/components/site/site-card';
import { vaccinePage } from '../content/vaccine-schedule-page';
import { formatThaiDay } from '../lib/calendar-date';
import type { PlannedVisit } from '../types/vaccine-schedule.types';
import { VisitStatusBadge } from './visit-status-badge';

/** The one thing to remember, in big type: when to bring the pet in next, and for what. */
export function NextVisitBanner({ visit, today }: { visit: PlannedVisit; today: string }) {
  const text = vaccinePage.result;
  // A missed visit is due now, whatever date it was first set for.
  const date = visit.status === 'overdue' ? today : visit.date;

  return (
    <SiteCard tone="forest" padding="lg" className="print:border print:border-ink-300 print:bg-white print:text-ink-900">
      <p className="text-sm font-semibold text-site-sun print:text-ink-700">{text.nextLabel}</p>
      <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-2">
        <p className="text-2xl font-semibold leading-tight sm:text-[28px]">{formatThaiDay(date)}</p>
        <VisitStatusBadge status={visit.status} />
      </div>
      <p className="mt-1.5 text-[15px] text-white/90 print:text-ink-700">{visit.title}</p>
    </SiteCard>
  );
}
