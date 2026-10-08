'use client';

import { Checkbox } from '@/components/ui/checkbox';
import { repeatText } from '../../content/advanced-booking';
import type { useConflictCheck } from '../../hooks/use-conflict-check';
import type { Expansion, RepeatRule } from '../../lib/recurrence';
import type { SeriesConflict } from '../../types/advanced-booking.types';
import { OccurrenceList } from './occurrence-list';
import { RepeatFields } from './repeat-fields';

interface RepeatSectionProps {
  /** `null` while booking a single visit. */
  rule: RepeatRule | null;
  firstDay: string;
  expansion: Expansion | null;
  refusals: Map<number, SeriesConflict>;
  hasVet: boolean;
  canCheck: boolean;
  check: ReturnType<typeof useConflictCheck>;
  onToggle: (on: boolean) => void;
  onChange: (rule: RepeatRule) => void;
}

/** Turns the booking into a course: the repeat rule, and every date it gives before anything is booked. */
export function RepeatSection({ rule, firstDay, expansion, refusals, hasVet, canCheck, check, onToggle, onChange }: RepeatSectionProps) {
  return (
    <fieldset className="space-y-3 rounded-lg border border-ink-200 px-3.5 py-3">
      <legend className="sr-only">{repeatText.legend}</legend>
      <Checkbox
        label={repeatText.toggle}
        hint={repeatText.toggleHint}
        checked={rule !== null}
        onChange={(event) => onToggle(event.target.checked)}
      />

      {rule && (
        <>
          <RepeatFields rule={rule} firstDay={firstDay} onChange={onChange} />
          <OccurrenceList expansion={expansion} refusals={refusals} hasVet={hasVet} canCheck={canCheck} check={check} />
        </>
      )}
    </fieldset>
  );
}
