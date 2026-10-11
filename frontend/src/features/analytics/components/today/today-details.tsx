import { todayCards } from '../../content/today-cards';
import type { TodayDashboard } from '../../types/analytics.types';
import { TodayOverview } from '../today-overview';

/**
 * "รายละเอียดวันนี้": every count of the day by status, folded away by default. The main numbers
 * are above it; this is for the person who wants the whole breakdown, one click away.
 */
export function TodayDetails({ today }: { today: TodayDashboard }) {
  return (
    <details className="group rounded-xl border border-ink-200 bg-white">
      <summary className="flex cursor-pointer list-none items-center justify-between gap-3 rounded-xl px-5 py-3.5 hover:bg-ink-50 [&::-webkit-details-marker]:hidden">
        <span>
          <span className="block font-display text-base font-semibold text-ink-900">{todayCards.heading}</span>
          <span className="block text-xs text-ink-500">{todayCards.hint}</span>
        </span>
        <span aria-hidden="true" className="text-ink-400 transition-transform group-open:rotate-90">
          ›
        </span>
      </summary>
      <div className="border-t border-ink-100 p-3">
        <TodayOverview today={today} />
      </div>
    </details>
  );
}
