import type { InventoryForecastItem } from '../../types/forecast.types';
import { ForecastRow } from './forecast-row';

interface ForecastListProps {
  rows: InventoryForecastItem[];
  leadTimeDays: number;
}

/** The forecast, one row per item, in the order the page chose. */
export function ForecastList({ rows, leadTimeDays }: ForecastListProps) {
  return (
    <ul className="divide-y divide-ink-100 rounded-xl border border-ink-200 bg-white">
      {rows.map((row) => (
        <ForecastRow key={row.item_id} row={row} leadTimeDays={leadTimeDays} />
      ))}
    </ul>
  );
}
