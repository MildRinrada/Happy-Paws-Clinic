import type { Branch, Holiday } from '@/features/clinic/types/clinic.types';
import { formatLongDate } from '@/lib/format/datetime';
import { aboutVisit } from '../content/about-page';

/** Special closures still to come, with the branch each one applies to. Nothing when there are none. */
export function HolidayList({ holidays, branches }: { holidays: Holiday[]; branches: Branch[] }) {
  if (holidays.length === 0) {
    return null;
  }

  const branchName = (id?: string) =>
    id ? (branches.find((branch) => branch.id === id)?.name ?? id) : aboutVisit.wholeClinic;

  return (
    <div className="rounded-2xl border border-site-alert/30 bg-white p-5">
      <h4 className="font-bold text-ink-900">{aboutVisit.holidaysLabel}</h4>
      <ul className="mt-2 space-y-1.5 text-[15px]">
        {holidays.map((holiday) => (
          <li key={holiday.id} className="flex flex-wrap justify-between gap-x-4">
            <span className="text-ink-800">
              {formatLongDate(holiday.date)} {holiday.name}
            </span>
            <span className="text-site-muted">{branchName(holiday.branch_id)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
