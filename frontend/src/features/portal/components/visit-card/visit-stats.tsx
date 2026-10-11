import type { VisitStat } from '../../lib/visit-stats';
import { QueueStat } from './queue-stat';

/** The two boxes side by side, equal widths. */
export function VisitStats({ stats }: { stats: VisitStat[] }) {
  return (
    <div className="grid grid-cols-2 gap-2.5">
      {stats.map((stat) => (
        <QueueStat key={stat.label} {...stat} />
      ))}
    </div>
  );
}
