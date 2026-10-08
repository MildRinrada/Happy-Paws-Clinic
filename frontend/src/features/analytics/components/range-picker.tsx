import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { dashboardPage } from '../content/dashboard-page';
import { presetRanges, reportHref, type ReportRange } from '../lib/report-range';

/**
 * The span every report on the tab covers. A plain GET form, so the range lives in the address:
 * a link to the page opens the same numbers, and the back button goes back to the last range.
 */
export function RangePicker({ range, today }: { range: ReportRange; today: string }) {
  const text = dashboardPage.range;

  return (
    <div className="flex flex-wrap items-end gap-3 rounded-xl border border-ink-200 bg-white px-4 py-3">
      <form action="/dashboard" method="get" className="flex flex-wrap items-end gap-3">
        <input type="hidden" name="tab" value={range.tab} />
        <label className="space-y-1 text-xs text-ink-600">
          <span className="block">{text.from}</span>
          <Input type="date" name="from" defaultValue={range.from} max={today} className="h-9 w-40" />
        </label>
        <label className="space-y-1 text-xs text-ink-600">
          <span className="block">{text.to}</span>
          <Input type="date" name="to" defaultValue={range.to} className="h-9 w-40" />
        </label>
        <label className="space-y-1 text-xs text-ink-600">
          <span className="block">{text.granularity}</span>
          <Select name="granularity" defaultValue={range.granularity} className="h-9 w-32">
            {(['day', 'week', 'month'] as const).map((granularity) => (
              <option key={granularity} value={granularity}>
                {text.granularities[granularity]}
              </option>
            ))}
          </Select>
        </label>
        <button type="submit" className={buttonClass('primary', 'sm')}>
          {text.apply}
        </button>
      </form>

      <div className="flex flex-wrap items-center gap-1.5">
        {presetRanges(today).map((preset) => {
          const active = preset.from === range.from && preset.to === range.to;

          return (
            <Link
              key={preset.key}
              href={reportHref(range, { from: preset.from, to: preset.to })}
              aria-current={active ? 'true' : undefined}
              className={buttonClass(active ? 'secondary' : 'ghost', 'sm')}
            >
              {text.presets[preset.key]}
            </Link>
          );
        })}
      </div>
      <p className="w-full text-xs text-ink-500">{text.note}</p>
    </div>
  );
}
