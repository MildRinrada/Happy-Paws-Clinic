import Link from 'next/link';
import { formatDateTime } from '@/lib/format/datetime';
import { segmentListText } from '../../content/segment-list';
import { crmPaths } from '../../lib/crm-paths';
import type { Segment } from '../../types/crm.types';

/** One saved group: its name, the counts it was saved with, and the way in to count it again. */
export function SegmentListRow({ segment }: { segment: Segment }) {
  const text = segmentListText;

  return (
    <li className="flex flex-wrap items-center justify-between gap-3 py-3">
      <div className="min-w-0">
        <p className="font-medium text-ink-900">{segment.name}</p>
        <p className="text-sm tabular-nums text-ink-600">
          {text.counts(segment.customer_count, segment.reachable_count)}
          <span className="ml-3 text-xs text-ink-500">{text.savedAt(formatDateTime(segment.created_at))}</span>
        </p>
      </div>
      <Link href={crmPaths.segment(segment.id)} className="text-sm text-brand-700 hover:underline">
        {text.open}
      </Link>
    </li>
  );
}
