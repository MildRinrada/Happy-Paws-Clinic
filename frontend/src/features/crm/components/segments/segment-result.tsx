import Link from 'next/link';
import { Button, buttonClass } from '@/components/ui/button';
import { segmentPage } from '../../content/segment-page';
import { crmPaths } from '../../lib/crm-paths';
import type { Segment } from '../../types/crm.types';

interface Props {
  segment: Segment;
  canBroadcast: boolean;
  onAnother: () => void;
}

/** How many customers the segment holds, how many a LINE broadcast would reach, and the way to send one. */
export function SegmentResult({ segment, canBroadcast, onAnother }: Props) {
  const text = segmentPage.result;
  const stats = [
    { label: text.customers, value: segment.customer_count },
    { label: text.reachable, value: segment.reachable_count },
  ];

  return (
    <div className="space-y-4">
      <p role="status" className="font-medium text-emerald-800">
        {text.heading(segment.name)}
      </p>

      <dl className="grid gap-3 sm:grid-cols-2">
        {stats.map((stat) => (
          <div key={stat.label} className="rounded-lg bg-ink-50 px-4 py-3">
            <dt className="text-sm text-ink-600">{stat.label}</dt>
            <dd className="font-display text-2xl font-semibold tabular-nums text-ink-900">{text.count(stat.value)}</dd>
          </div>
        ))}
      </dl>
      <p className="text-sm text-ink-500">{text.reachableHint}</p>

      <div className="flex flex-wrap items-center gap-3">
        {canBroadcast && segment.reachable_count > 0 && (
          <Link
            href={crmPaths.broadcastToSegment({ id: segment.id, name: segment.name, reachable: segment.reachable_count })}
            className={buttonClass('primary', 'sm')}
          >
            {text.broadcast}
          </Link>
        )}
        {canBroadcast && segment.reachable_count === 0 && <p className="text-sm text-ink-600">{text.noneReachable}</p>}
        <Button size="sm" variant="secondary" onClick={onAnother}>
          {text.another}
        </Button>
      </div>
    </div>
  );
}
