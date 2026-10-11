import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { reportsPage } from '../content/reports-page';
import { presetRanges, reportHref, REPORTS_PATH, type ReportRange } from '../lib/report-range';

const NOTE_ID = 'report-range-note';

/**
 * The span every report on the tab covers. A plain GET form, so the range lives in the address:
 * a link to the page opens the same numbers, and the back button goes back to the last range.
 *
 * The 366-day limit is one small line under the dates, tied to both date inputs so a screen reader
 * hears it with the field. The page clamps a longer span rather than refusing it.
 */
export function RangePicker({ range, today }: { range: ReportRange; today: string }) {
  const text = reportsPage.range;

  return (
    <div className="space-y-1.5 rounded-xl border border-ink-200 bg-white px-4 py-3">
      <div className="flex flex-wrap items-end gap-3">
        <form action={REPORTS_PATH} method="get" className="flex flex-wrap items-end gap-3">
          <input type="hidden" name="tab" value={range.tab} />
          <label className="space-y-1 text-xs text-ink-600">
            <span className="block">{text.from}</span>
            <Input type="date" name="from" defaultValue={range.from} max={today} aria-describedby={NOTE_ID} className="h-9 w-40" />
          </label>
          <label className="space-y-1 text-xs text-ink-600">
            <span className="block">{text.to}</span>
            <Input type="date" name="to" defaultValue={range.to} aria-describedby={NOTE_ID} className="h-9 w-40" />
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

        <nav aria-label={text.presetsLabel} className="flex flex-wrap items-center gap-1.5">
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
        </nav>
      </div>
      <p id={NOTE_ID} className="text-[11px] text-ink-500">
        {text.note}
      </p>
    </div>
  );
}
