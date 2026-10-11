import { petDetail } from '../../../content/pet-detail';
import type { PetVaccine, PetVaccineStatus } from '../../../lib/load-pet-vaccines';
import { thaiDate } from '../../../lib/pet-dates';
import { VaccineStatusPill } from './vaccine-status-pill';

const dots: Record<PetVaccineStatus, string> = {
  overdue: 'bg-red-700',
  due_soon: 'bg-amber-500',
  given: 'bg-brand-600',
};

/**
 * One vaccine: its name, the day it is due or was given, and its status. On a phone it is a step of
 * a timeline, a coloured dot joined to the next by a line; from 1024px a row between rules.
 */
export function VaccineItem({ vaccine, last }: { vaccine: PetVaccine; last: boolean }) {
  const text = petDetail.vaccines;
  const date = thaiDate(vaccine.date);

  return (
    <li className="relative flex items-start justify-between gap-3 pb-4 pl-[26px] last:pb-0 lg:border-b lg:border-ink-100 lg:py-3 lg:pl-0 lg:last:border-b-0 lg:last:pb-3">
      <span aria-hidden="true" className={`absolute left-0 top-[5px] size-3.5 rounded-full lg:hidden ${dots[vaccine.status]}`} />
      {!last && <span aria-hidden="true" className="absolute bottom-0 left-1.5 top-6 w-0.5 bg-ink-200 lg:hidden" />}

      <div className="min-w-0">
        <p className="break-words font-display text-[15px] font-medium leading-6 text-ink-900 lg:text-[13.5px]">{vaccine.name}</p>
        <p className="text-[13px] leading-5 text-ink-600 lg:text-xs">
          {vaccine.status === 'given' ? text.givenOn(date) : text.dueOn(date)}
        </p>
      </div>
      <VaccineStatusPill status={vaccine.status} />
    </li>
  );
}
