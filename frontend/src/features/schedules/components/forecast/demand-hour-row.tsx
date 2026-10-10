import { Badge } from '@/components/ui/badge';
import { demandForecastPage } from '../../content/forecast-page';
import type { DemandHour } from '../../types/forecast.types';

/** One clinic hour: who is booked, who may walk in, and how many vets share them. */
export function DemandHourRow({ hour }: { hour: DemandHour }) {
  const text = demandForecastPage;

  return (
    <li className={`flex flex-wrap items-center gap-x-3 gap-y-1 px-3 py-2 text-sm ${hour.high_load ? 'bg-red-50' : ''}`}>
      <span className="w-12 font-medium tabular-nums text-ink-900">{text.hour(hour.hour)}</span>
      <span className="tabular-nums text-ink-700">{text.booked(hour.booked_appointments)}</span>
      <span className="tabular-nums text-ink-700">{text.walkIns(hour.expected_walk_ins)}</span>
      <span className={`tabular-nums ${hour.vets_on_shift === 0 ? 'text-red-700' : 'text-ink-700'}`}>
        {hour.vets_on_shift === 0 ? text.noVet : text.vets(hour.vets_on_shift)}
      </span>
      {hour.load_per_vet !== undefined && <span className="tabular-nums text-ink-500">{text.perVet(hour.load_per_vet)}</span>}
      {hour.high_load && <Badge tone="red">{text.busy}</Badge>}
    </li>
  );
}
