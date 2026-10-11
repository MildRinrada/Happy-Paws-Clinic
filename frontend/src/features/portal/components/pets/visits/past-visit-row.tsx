import { petDetail } from '../../../content/pet-detail';
import type { PastVisitView } from '../../../lib/load-pet-visits';
import { thaiDate } from '../../../lib/pet-dates';

/** One visit: the service, then "มาตรวจ 14 มี.ค. 2569 สพ.ญ. มินตรา". */
export function PastVisitRow({ visit }: { visit: PastVisitView }) {
  const text = petDetail.visits;

  return (
    <li className="py-3.5 lg:py-3">
      <p className="break-words font-display text-[15px] font-medium leading-6 text-ink-900 lg:text-[13.5px]">
        {visit.serviceName ?? text.serviceFallback}
      </p>
      <p className="text-[13px] leading-5 text-ink-600 lg:text-xs">{text.line(thaiDate(visit.startsAt), visit.vetName)}</p>
    </li>
  );
}
