import { Button } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { demandForecastPage, FORECAST_SPANS } from '../../content/forecast-page';

interface DemandRangeFormProps {
  from: string;
  days: number;
  today: string;
}

/** Which days to forecast. A GET form, so the choice stays in the address. */
export function DemandRangeForm({ from, days, today }: DemandRangeFormProps) {
  const text = demandForecastPage;

  return (
    <form method="get" action="/schedules/forecast" className="flex flex-wrap items-end gap-3">
      <label className="block w-44 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.from}</span>
        <Input type="date" name="from" min={today} defaultValue={from} />
      </label>
      <label className="block w-28 space-y-1.5 text-sm font-medium text-ink-700">
        <span>{text.span}</span>
        <Select name="days" defaultValue={String(days)}>
          {FORECAST_SPANS.map((span) => (
            <option key={span} value={span}>
              {text.spanOption(span)}
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
