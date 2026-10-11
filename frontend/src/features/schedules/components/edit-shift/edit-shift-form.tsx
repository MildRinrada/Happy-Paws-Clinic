'use client';

import { useState, type FormEvent } from 'react';
import { Field, Input } from '@/components/ui/field';
import { clinicInstant } from '@/features/booking/lib/booking-dates';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { changeShiftAction } from '../../actions/schedule-actions';
import { shiftEditText } from '../../content/shift-edit';
import { dayLabel } from '../../lib/chip-label';
import { clinicClock, clinicDate } from '../../lib/clinic-clock';
import type { ShiftChip } from '../../lib/shift-chips';
import type { Shift, TimeOffConflict } from '../../types/schedule.types';
import { RemoveShiftControls } from './remove-shift-controls';
import { ShiftBlockPicker } from './shift-block-picker';
import { ShiftChangeResult } from './shift-change-result';

interface EditShiftFormProps {
  chip: ShiftChip;
  /** Whose shift, for the line at the top. */
  name: string;
  /** The roster day the chip was on. */
  day: string;
  /** Staff id to name, for the substitutes suggested. */
  names: Record<string, string>;
  /** Shuts the dialog. */
  onDone: () => void;
}

const timesOf = (block: Shift) => ({ start: clinicClock(block.starts_at), end: clinicClock(block.ends_at) });

/**
 * Change the times of one block of a shift, or remove it (docs/contract-extensions.md § 47). A
 * block under way keeps its start, and one that has ended is not offered. When nothing booked is
 * left outside the shift, the dialog just shuts; otherwise it lists what needs a call.
 */
export function EditShiftForm({ chip, name, day, names, onDone }: EditShiftFormProps) {
  const text = shiftEditText;
  // Read once as the dialog opens: the form only ever renders in the browser, after a click.
  const [now] = useState(() => Date.now());
  const [block, setBlock] = useState(() => chip.blocks.find((each) => Date.parse(each.ends_at) > now) ?? chip.blocks[0]!);
  const [times, setTimes] = useState(() => timesOf(block));
  const [outcome, setOutcome] = useState<{ done: string; conflicts: TimeOffConflict[] }>();
  const { errors, message, pending, submit } = useFormSubmit(formMessages);
  const started = Date.parse(block.starts_at) <= now;

  function pick(next: Shift) {
    setBlock(next);
    setTimes(timesOf(next));
  }

  function settle(done: string, conflicts: TimeOffConflict[]) {
    if (conflicts.length === 0) onDone();
    else setOutcome({ done, conflicts });
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () =>
        changeShiftAction({
          id: block.id,
          version: block.version,
          starts_at: clinicInstant(clinicDate(block.starts_at), times.start),
          ends_at: clinicInstant(clinicDate(block.ends_at), times.end),
        }),
      (result) => settle(text.saved, result.conflicts),
    );
  }

  if (outcome) {
    return <ShiftChangeResult done={outcome.done} conflicts={outcome.conflicts} names={names} onClose={onDone} />;
  }

  return (
    <div className="space-y-4">
      <p className="text-sm font-medium text-ink-800">{text.who(name, dayLabel(day))}</p>
      {chip.blocks.length > 1 && <ShiftBlockPicker blocks={chip.blocks} selectedId={block.id} now={now} onPick={pick} />}

      <form onSubmit={save} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label={text.start} htmlFor="edit-shift-start" error={errors.starts_at}>
            <Input
              id="edit-shift-start"
              type="time"
              value={times.start}
              required
              disabled={started}
              onChange={(event) => setTimes({ ...times, start: event.target.value })}
            />
          </Field>
          <Field label={text.end} htmlFor="edit-shift-end" error={errors.ends_at}>
            <Input id="edit-shift-end" type="time" value={times.end} required onChange={(event) => setTimes({ ...times, end: event.target.value })} />
          </Field>
        </div>
        {started && <p className="text-sm text-amber-800">{text.started}</p>}
        <FormFooter pending={pending} message={message} submitLabel={text.save} pendingLabel={text.saving} />
      </form>

      <RemoveShiftControls selected={block} blocks={chip.blocks} now={now} onRemoved={(conflicts) => settle(text.removed, conflicts)} />
    </div>
  );
}
