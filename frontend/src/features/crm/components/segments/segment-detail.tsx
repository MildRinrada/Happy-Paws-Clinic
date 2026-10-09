import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatDateTime } from '@/lib/format/datetime';
import { segmentListText } from '../../content/segment-list';
import { segmentPage } from '../../content/segment-page';
import { crmPaths } from '../../lib/crm-paths';
import type { Segment } from '../../types/crm.types';
import { SegmentCriteriaSummary } from './segment-criteria-summary';

interface SegmentDetailProps {
  segment: Segment;
  canBroadcast: boolean;
  branchName?: string;
}

/** One group counted now: how many match, how many a LINE broadcast reaches, the conditions, and the way to send. */
export function SegmentDetail({ segment, canBroadcast, branchName }: SegmentDetailProps) {
  const result = segmentPage.result;
  const text = segmentListText.detail;
  const stats = [
    { label: result.customers, value: segment.customer_count },
    { label: result.reachable, value: segment.reachable_count },
  ];

  return (
    <Card>
      <CardHeader title={segment.name} aside={<span className="text-xs text-ink-500">{text.countedAt(formatDateTime(segment.counted_at))}</span>} />
      <CardBody className="space-y-4">
        <dl className="grid gap-3 sm:grid-cols-2">
          {stats.map((stat) => (
            <div key={stat.label} className="rounded-lg bg-ink-50 px-4 py-3">
              <dt className="text-sm text-ink-600">{stat.label}</dt>
              <dd className="font-display text-2xl font-semibold tabular-nums text-ink-900">{result.count(stat.value)}</dd>
            </div>
          ))}
        </dl>
        <p className="text-sm text-ink-500">{result.reachableHint}</p>

        <div className="space-y-1.5">
          <h3 className="text-sm font-medium text-ink-800">{text.criteria}</h3>
          <SegmentCriteriaSummary criteria={segment.criteria} branchName={branchName} />
        </div>

        {canBroadcast && segment.reachable_count > 0 && (
          <Link
            href={crmPaths.broadcastToSegment({ id: segment.id, name: segment.name, reachable: segment.reachable_count })}
            className={buttonClass('primary', 'sm')}
          >
            {result.broadcast}
          </Link>
        )}
        {canBroadcast && segment.reachable_count === 0 && <p className="text-sm text-ink-600">{result.noneReachable}</p>}
      </CardBody>
    </Card>
  );
}
