'use client';

import { Button } from '@/components/ui/button';
import { conflictCheckText } from '../../content/advanced-booking';
import { newAppointment } from '../../content/appointment-calendar';
import type { useConflictCheck } from '../../hooks/use-conflict-check';
import { ConflictLines } from './conflict-lines';

interface SingleConflictCheckProps {
  startsAt: string;
  hasVet: boolean;
  check: ReturnType<typeof useConflictCheck>;
}

/** "Is this time all right?" for one booking, before pressing book. */
export function SingleConflictCheck({ startsAt, hasVet, check }: SingleConflictCheckProps) {
  const text = conflictCheckText;
  const report = check.reports?.get(Date.parse(startsAt));

  return (
    <div className="space-y-2 rounded-lg border border-ink-200 bg-ink-50/40 px-3.5 py-3">
      <div className="flex flex-wrap items-center gap-3">
        <Button variant="secondary" size="sm" disabled={check.checking} onClick={() => check.check(newAppointment.unreachable)}>
          {check.checking ? text.checking : text.check}
        </Button>
        {report?.clear && <p role="status" className="text-sm font-medium text-emerald-700">{text.clear}</p>}
        {report && !report.clear && <p role="status" className="text-sm font-medium text-amber-800">{text.found(report.conflicts.length)}</p>}
        {check.stale && !check.checking && <p className="text-sm text-ink-500">{text.stale}</p>}
      </div>
      {check.error && <p role="alert" className="text-sm text-red-700">{check.error}</p>}
      {report && !report.clear && <ConflictLines conflicts={report.conflicts} />}
      <p className="text-xs text-ink-500">{hasVet ? text.reportOnly : `${text.noVet} ${text.reportOnly}`}</p>
    </div>
  );
}
