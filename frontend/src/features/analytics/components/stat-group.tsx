import type { ReactNode } from 'react';
import { Card } from '@/components/ui/card';

/** A titled card of stat tiles. */
export function StatGroup({ title, aside, children }: { title: string; aside?: ReactNode; children: ReactNode }) {
  return (
    <Card className="p-4">
      <div className="mb-3 flex items-baseline justify-between gap-3">
        <h3 className="text-sm font-semibold text-ink-900">{title}</h3>
        {aside && <span className="text-xs text-ink-500">{aside}</span>}
      </div>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">{children}</div>
    </Card>
  );
}
