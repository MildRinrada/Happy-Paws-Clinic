import { portalHome } from '../../content/portal-home';
import { formatDueDay } from '../../lib/home-dates';
import type { VaccinationDue } from '../../types/portal.types';
import { EmptyLine } from '../shared/empty-line';
import { SectionCard } from '../shared/section-card';
import { VaccineBadge } from './vaccine-badge';

/** Vaccines overdue or due within the month, most urgent first (the backend sorts them). */
export function VaccinesDue({ items }: { items: VaccinationDue[] }) {
  const text = portalHome.vaccines;

  return (
    <SectionCard title={text.title} ruled>
      {items.length === 0 ? (
        <div className="py-4">
          <EmptyLine>{text.empty}</EmptyLine>
        </div>
      ) : (
        <ul className="divide-y divide-ink-100">
          {items.map((item) => (
            <li key={`${item.pet_id}-${item.vaccine_name}`} className="flex items-center justify-between gap-3 pb-3 pt-3.5">
              <div className="min-w-0">
                <p className="font-display text-[13.5px] font-medium leading-6 text-ink-900">{text.forPet(item.vaccine_name, item.pet_name)}</p>
                <p className="text-xs leading-5 text-ink-600">{text.dueOn(formatDueDay(item.next_due_on))}</p>
              </div>
              <VaccineBadge status={item.status} />
            </li>
          ))}
        </ul>
      )}
    </SectionCard>
  );
}
