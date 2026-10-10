import { Badge } from '@/components/ui/badge';
import { forecastPage } from '../../content/forecast-page';
import { formatDay } from '../../lib/format-day';
import { runsOutBeforeDelivery } from '../../lib/forecast-query';
import type { InventoryForecastItem } from '../../types/forecast.types';
import { ForecastConfidenceBadge } from './forecast-confidence-badge';

interface ForecastRowProps {
  row: InventoryForecastItem;
  leadTimeDays: number;
}

/** One item: when it runs out, how fast it goes, and what to order. */
export function ForecastRow({ row, leadTimeDays }: ForecastRowProps) {
  const text = forecastPage;
  const urgent = runsOutBeforeDelivery(row, leadTimeDays);

  return (
    <li className="flex flex-wrap items-start justify-between gap-3 px-4 py-3">
      <div className="min-w-0 flex-1 space-y-0.5">
        <div className="flex flex-wrap items-center gap-2">
          <p className="break-words font-medium text-ink-900">{row.name}</p>
          <ForecastConfidenceBadge confidence={row.confidence} />
        </div>
        {row.days_until_out !== undefined ? (
          <p className={`text-sm ${urgent ? 'font-medium text-red-700' : 'text-ink-800'}`}>
            {text.outIn(row.days_until_out)} <span className="text-ink-500">({text.outOn(formatDay(row.runs_out_on))})</span>
          </p>
        ) : (
          <p className="text-sm text-ink-500">{text.notUsed}</p>
        )}
        <p className="text-xs tabular-nums text-ink-600">{text.rate(row.avg_daily_30d, row.avg_daily_90d, row.unit)}</p>
        <p className="text-xs tabular-nums text-ink-600">
          {text.stock(row.available, row.unit)}
          {row.on_order > 0 && <span className="ml-2">{text.onOrder(row.on_order)}</span>}
          <span className="ml-2 text-ink-400">{text.history(row.history_days)}</span>
        </p>
      </div>
      <div className="flex flex-col items-end gap-1 text-right">
        {row.suggested_order_qty > 0 ? (
          <p className="text-sm font-semibold tabular-nums text-brand-800">{text.suggest(row.suggested_order_qty, row.unit)}</p>
        ) : (
          <p className="text-sm text-ink-500">{text.noOrder}</p>
        )}
        {urgent && <Badge tone="red">{text.urgent}</Badge>}
      </div>
    </li>
  );
}
