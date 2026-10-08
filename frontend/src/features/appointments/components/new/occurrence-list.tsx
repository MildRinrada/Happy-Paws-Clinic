'use client';

import { Button } from '@/components/ui/button';
import { conflictCheckText, repeatText } from '../../content/advanced-booking';
import { newAppointment } from '../../content/appointment-calendar';
import type { useConflictCheck } from '../../hooks/use-conflict-check';
import type { Expansion } from '../../lib/recurrence';
import type { SeriesConflict } from '../../types/advanced-booking.types';
import { OccurrenceRow } from './occurrence-row';

interface OccurrenceListProps {
  expansion: Expansion | null;
  /** Per-date refusals from the last attempt to book, keyed by the start's time in ms. */
  refusals: Map<number, SeriesConflict>;
  hasVet: boolean;
  /** `appointment:read`, which the conflict check needs. */
  canCheck: boolean;
  check: ReturnType<typeof useConflictCheck>;
}

/** Every date the course would book, with what a check or a refused booking said about each. */
export function OccurrenceList({ expansion, refusals, hasVet, canCheck, check }: OccurrenceListProps) {
  if (!expansion) {
    return <p className="text-sm text-ink-500">{repeatText.previewEmpty}</p>;
  }

  const { starts, problem } = expansion;
  const checkable = canCheck && !problem && starts.length > 0;

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-sm font-medium text-ink-700">{repeatText.previewTitle(starts.length)}</p>
        {checkable && (
          <Button variant="secondary" size="sm" disabled={check.checking} onClick={() => check.check(newAppointment.unreachable)}>
            {check.checking ? conflictCheckText.checking : conflictCheckText.checkAll}
          </Button>
        )}
      </div>

      {problem && <p role="alert" className="text-sm text-red-700">{repeatText[problem]}</p>}
      {check.error && <p role="alert" className="text-sm text-red-700">{check.error}</p>}
      {check.stale && !check.checking && <p className="text-sm text-ink-500">{conflictCheckText.stale}</p>}

      {starts.length > 0 && (
        <ol className="max-h-80 space-y-1.5 overflow-y-auto pr-1">
          {starts.map((startsAt, at) => (
            <OccurrenceRow
              key={startsAt}
              index={at + 1}
              startsAt={startsAt}
              report={check.reports?.get(Date.parse(startsAt))}
              refusal={refusals.get(Date.parse(startsAt))}
            />
          ))}
        </ol>
      )}

      <ul className="list-disc space-y-0.5 pl-5 text-xs text-ink-500">
        <li>{repeatText.allOrNothing}</li>
        <li>{repeatText.oneVet}</li>
        {canCheck && <li>{hasVet ? conflictCheckText.reportOnly : `${conflictCheckText.noVet} ${conflictCheckText.reportOnly}`}</li>}
      </ul>
    </div>
  );
}
