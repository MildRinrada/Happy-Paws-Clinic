import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { attentionBox } from '../../content/attention-box';
import type { AttentionView } from '../../types/today.types';
import { AttentionRow } from './attention-row';

/**
 * "ต้องจัดการตอนนี้" at the top of the page: only the kinds of work that have something waiting.
 * With nothing waiting it says so plainly. A line that could not be read is not drawn, and the box
 * says that some lines are missing, so "เรียบร้อยดี" is never claimed over a read that failed silently.
 */
export function AttentionBox({ view }: { view: AttentionView }) {
  const text = attentionBox;

  return (
    <Card className="h-full">
      <CardHeader title={text.heading} />
      <CardBody className="space-y-2 px-2 py-2">
        {view.items.length > 0 ? (
          <ul className="divide-y divide-ink-100">
            {view.items.map((item) => (
              <AttentionRow key={item.key} item={item} />
            ))}
          </ul>
        ) : (
          view.checked > 0 && <p className="px-3 py-3 text-sm font-medium text-emerald-800">{text.allClear}</p>
        )}
        {view.failed > 0 && <p className="px-3 pb-1 text-xs text-ink-500">{text.partly}</p>}
      </CardBody>
    </Card>
  );
}
