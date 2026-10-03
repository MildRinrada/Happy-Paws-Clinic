import { formatLongDate } from '@/lib/format/datetime';
import { portalHome } from '../../content/portal-home';
import type { VaccinationDue } from '../../types/portal.types';
import { EmptyLine } from '../shared/empty-line';
import { SectionCard } from '../shared/section-card';

/** Vaccines overdue or due within the month, most urgent first (the backend sorts them). */
export function VaccinesDue({ items }: { items: VaccinationDue[] }) {
  const text = portalHome.vaccines;

  return (
    <SectionCard title={text.title}>
      {items.length === 0 ? (
        <EmptyLine>{text.empty}</EmptyLine>
      ) : (
        <ul className="divide-y divide-ink-100">
          {items.map((item) => (
            <li key={`${item.pet_id}-${item.vaccine_name}`} className="flex items-center justify-between gap-3 py-2.5">
              <div className="min-w-0">
                <p className="text-sm font-medium text-ink-900">
                  {item.vaccine_name} <span className="font-normal text-site-muted">({item.pet_name})</span>
                </p>
                <p className="text-xs text-site-muted">{text.dueOn(formatLongDate(item.next_due_on))}</p>
              </div>
              <span
                className={`shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold ${
                  item.status === 'overdue' ? 'bg-red-50 text-red-700' : 'bg-amber-50 text-amber-800'
                }`}
              >
                {item.status === 'overdue' ? text.overdue : text.dueSoon}
              </span>
            </li>
          ))}
        </ul>
      )}
    </SectionCard>
  );
}
