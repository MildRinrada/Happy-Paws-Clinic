import { Button } from '@/components/ui/button';
import { Select } from '@/components/ui/field';
import { forecastPage, forecastSortLabels } from '../../content/forecast-page';
import { COVER_CHOICES, FORECAST_SORTS, type ForecastSort } from '../../lib/forecast-query';
import { inventoryPaths } from '../../lib/inventory-paths';

interface ForecastOptionsFormProps {
  coverDays: number;
  sort: ForecastSort;
}

/** The cover period and the order. A GET form, so the choice stays in the address. */
export function ForecastOptionsForm({ coverDays, sort }: ForecastOptionsFormProps) {
  const text = forecastPage;

  return (
    <form method="get" action={inventoryPaths.forecast} className="flex flex-wrap items-end gap-3">
      <label className="block w-36 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.cover}</span>
        <Select name="cover_days" defaultValue={String(coverDays)}>
          {COVER_CHOICES.map((days) => (
            <option key={days} value={days}>
              {text.coverOption(days)}
            </option>
          ))}
        </Select>
      </label>
      <label className="block w-36 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.sort}</span>
        <Select name="sort" defaultValue={sort}>
          {FORECAST_SORTS.map((value) => (
            <option key={value} value={value}>
              {forecastSortLabels[value]}
            </option>
          ))}
        </Select>
      </label>
      <Button type="submit" variant="secondary">
        {text.apply}
      </Button>
    </form>
  );
}
