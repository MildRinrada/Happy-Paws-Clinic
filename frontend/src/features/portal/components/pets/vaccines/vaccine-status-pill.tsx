import { petDetail } from '../../../content/pet-detail';
import type { PetVaccineStatus } from '../../../lib/load-pet-vaccines';

const looks: Record<PetVaccineStatus, string> = {
  overdue: 'bg-red-100 text-red-800',
  due_soon: 'bg-amber-100 text-amber-900',
  given: 'bg-emerald-100 text-emerald-800',
};

const words: Record<PetVaccineStatus, string> = {
  overdue: petDetail.vaccines.overdue,
  due_soon: petDetail.vaccines.dueSoon,
  given: petDetail.vaccines.given,
};

/** "เลยกำหนด" in red, "ใกล้ครบกำหนด" in yellow, "ฉีดแล้ว" in green. */
export function VaccineStatusPill({ status }: { status: PetVaccineStatus }) {
  return (
    <span className={`shrink-0 whitespace-nowrap rounded-full px-3 font-display text-sm leading-6 lg:px-2.5 lg:text-xs ${looks[status]}`}>
      {words[status]}
    </span>
  );
}
