import { StatTile } from '@/features/analytics/components/stat-tile';
import { formatBaht } from '@/lib/format/money';
import { dailyBriefText } from '../../content/daily-brief';
import type { BriefNumbers as Numbers } from '../../types/staff-ai.types';

/**
 * The brief's figures, computed by the system rather than written by the model. Yesterday's
 * takings appear only when the API sent them, which it does for a session that may read money.
 */
export function BriefNumbers({ numbers }: { numbers: Numbers }) {
  const text = dailyBriefText;
  const { appointments, queue, inventory, yesterday_revenue: revenue } = numbers;

  return (
    <div className="grid grid-cols-2 gap-2 lg:grid-cols-4">
      <StatTile
        label={text.appointments}
        value={appointments.total}
        hint={`${text.upcoming(appointments.upcoming)} ${text.walkIn(appointments.walk_in)}`}
      />
      <StatTile label={text.queue} value={queue.tickets} hint={text.outstanding(queue.outstanding)} />
      <StatTile
        label={text.stock}
        value={inventory.expired + inventory.out_of_stock + inventory.near_expiry + inventory.low_stock}
        hint={text.stockHint(inventory.expired, inventory.out_of_stock)}
      />
      {revenue && <StatTile label={text.revenue} value={formatBaht(revenue.net_satang)} hint={text.revenueHint(revenue.payments)} />}
    </div>
  );
}
