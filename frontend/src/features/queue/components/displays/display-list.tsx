import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { EmptyState } from '@/components/ui/empty-state';
import { queueDisplays } from '../../content/queue-displays';
import type { QueueDisplaySummary } from '../../types/queue.types';
import { DisplayRow } from './display-row';

const text = queueDisplays.list;

/** The branch's screens, newest first, stopped ones included. Never a token or a code. */
export function DisplayList({ displays }: { displays: QueueDisplaySummary[] }) {
  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        {displays.length === 0 ? (
          <EmptyState title={text.empty} />
        ) : (
          <div className="overflow-x-auto rounded-xl border border-ink-200 bg-white">
            <table className="w-full min-w-[640px] text-sm">
              <thead className="border-b border-ink-100 text-left text-ink-500">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">{text.columns.name}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{text.columns.status}</th>
                  <th scope="col" className="px-4 py-3 font-medium">{text.columns.dates}</th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    <span className="sr-only">{queueDisplays.revoke.button}</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-ink-100">
                {displays.map((display) => (
                  <DisplayRow key={display.id} display={display} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardBody>
    </Card>
  );
}
