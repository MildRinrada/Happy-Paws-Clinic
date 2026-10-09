import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { segmentListText } from '../../content/segment-list';
import type { Segment } from '../../types/crm.types';
import { PageLinks } from '../shared/page-links';
import { SegmentListRow } from './segment-list-row';

interface SegmentListProps {
  segments: Segment[];
  nextHref?: string;
  firstHref?: string;
}

/** The saved groups, newest first. The counts are the ones taken at saving; opening one counts again. */
export function SegmentList({ segments, nextHref, firstHref }: SegmentListProps) {
  const text = segmentListText;

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-600">{text.hint}</p>
        {segments.length === 0 ? (
          <p className="text-sm text-ink-500">{text.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100">
            {segments.map((segment) => (
              <SegmentListRow key={segment.id} segment={segment} />
            ))}
          </ul>
        )}
        <PageLinks nextHref={nextHref} firstHref={firstHref} nextLabel={text.next} firstLabel={text.first} />
      </CardBody>
    </Card>
  );
}
