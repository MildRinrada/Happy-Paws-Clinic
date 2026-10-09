import { Button } from '@/components/ui/button';
import { lineBroadcastPage } from '../content/line-broadcast';
import type { BroadcastSegment } from '../types/line.types';

interface Props {
  segment: BroadcastSegment;
  onClear: () => void;
}

/** The CRM segment this broadcast goes to, in place of the customer picker, with the way back to it. */
export function BroadcastSegmentTarget({ segment, onClear }: Props) {
  const text = lineBroadcastPage;

  return (
    <div className="space-y-2 rounded-lg bg-brand-50 px-4 py-3 ring-1 ring-brand-200">
      <p className="font-medium text-brand-800">{text.segmentTarget(segment.name, segment.reachable)}</p>
      <p className="text-sm text-ink-600">{text.segmentHint}</p>
      <Button size="sm" variant="secondary" onClick={onClear}>
        {text.segmentClear}
      </Button>
    </div>
  );
}
