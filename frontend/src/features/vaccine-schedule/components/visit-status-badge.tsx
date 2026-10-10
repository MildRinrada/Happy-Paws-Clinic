import { vaccinePage } from '../content/vaccine-schedule-page';
import type { VisitStatus } from '../types/vaccine-schedule.types';

const tones: Record<VisitStatus, string> = {
  done: 'bg-ink-100 text-ink-600',
  overdue: 'bg-[#fde9e4] text-[#b42318]',
  due: 'bg-[#fff1dc] text-[#9a5b00]',
  upcoming: 'bg-brand-50 text-brand-700',
};

/** Where a visit stands: done, overdue, due soon or later. Words as well as colour, for print and for everyone. */
export function VisitStatusBadge({ status }: { status: VisitStatus }) {
  return (
    <span
      className={`inline-flex h-7 items-center whitespace-nowrap rounded-full px-3 text-[13px] font-semibold print:border print:border-current ${tones[status]}`}
    >
      {vaccinePage.status[status]}
    </span>
  );
}
