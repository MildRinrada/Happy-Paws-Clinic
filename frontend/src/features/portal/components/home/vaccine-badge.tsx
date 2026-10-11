import { portalHome } from '../../content/portal-home';
import type { VaccinationDue } from '../../types/portal.types';

const tones: Record<VaccinationDue['status'], string> = {
  overdue: 'bg-red-100 text-red-800',
  due_soon: 'bg-amber-100 text-amber-900',
};

/** "เลยกำหนด" in red or "ใกล้ครบกำหนด" in yellow. */
export function VaccineBadge({ status }: { status: VaccinationDue['status'] }) {
  const text = portalHome.vaccines;

  return (
    <span className={`shrink-0 whitespace-nowrap rounded-full px-2.5 text-xs leading-6 ${tones[status]}`}>
      {status === 'overdue' ? text.overdue : text.dueSoon}
    </span>
  );
}
