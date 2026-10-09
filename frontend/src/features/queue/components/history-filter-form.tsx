import { buttonClass } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { queueHistory } from '../content/queue-history';
import { HISTORY_FILTERS, type HistoryQuery } from '../lib/history-filters';

const text = queueHistory.filters;

/**
 * The date picker and status filter. A plain GET form: the choice lands in the address bar, so a
 * day can be bookmarked or sent to a colleague, and it works before any script has loaded.
 * The ids are fixed because the form appears once per page.
 */
export function HistoryFilterForm({ query, today }: { query: HistoryQuery; today: string }) {
  return (
    <form method="get" className="flex flex-wrap items-end gap-3">
      <div className="space-y-1.5">
        <label htmlFor="queue-history-date" className="block text-sm font-medium text-ink-700">
          {text.date}
        </label>
        <Input id="queue-history-date" type="date" name="date" max={today} defaultValue={query.date} className="w-44" />
      </div>

      <div className="space-y-1.5">
        <label htmlFor="queue-history-status" className="block text-sm font-medium text-ink-700">
          {text.status}
        </label>
        <Select id="queue-history-status" name="status" defaultValue={query.filter} className="w-44">
          {HISTORY_FILTERS.map((filter) => (
            <option key={filter} value={filter}>
              {queueHistory.statusOptions[filter]}
            </option>
          ))}
        </Select>
      </div>

      <button type="submit" className={buttonClass('secondary')}>
        {text.submit}
      </button>
    </form>
  );
}
