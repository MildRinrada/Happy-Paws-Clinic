import { formatDateTime } from '@/lib/format/datetime';
import { automationsPage } from '../content/automations-page';
import type { AutomationStats } from '../types/automation.types';

/** "สถิติการยิงย้อนหลัง" in one line: outcomes, the last 30 days, and when it last fired. */
export function AutomationStatsLine({ stats }: { stats: AutomationStats }) {
  const text = automationsPage.stats;

  if (stats.total === 0) {
    return <p className="text-xs text-ink-500">{text.never}</p>;
  }

  return (
    <p className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-ink-600">
      <span>
        {text.sent} {stats.sent}
      </span>
      <span>
        {text.skipped} {stats.skipped}
      </span>
      <span>
        {text.failed} {stats.failed + stats.lost}
      </span>
      <span>
        {text.recent} {stats.last_30_days}
      </span>
      {stats.last_fired_at && (
        <span>
          {text.lastFired} {formatDateTime(stats.last_fired_at)}
        </span>
      )}
    </p>
  );
}
