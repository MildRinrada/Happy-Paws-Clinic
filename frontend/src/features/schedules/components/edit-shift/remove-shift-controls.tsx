'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { removeShiftAction } from '../../actions/schedule-actions';
import { shiftEditText } from '../../content/shift-edit';
import type { Shift, TimeOffConflict } from '../../types/schedule.types';

interface RemoveShiftControlsProps {
  /** The block picked in the dialog. */
  selected: Shift;
  /** Every block of the shift, for "remove the whole shift" when there is more than one. */
  blocks: Shift[];
  /** Now, in milliseconds: a block that has begun cannot be removed. */
  now: number;
  /** Called with the appointments that were in what was removed. */
  onRemoved: (conflicts: TimeOffConflict[]) => void;
}

/**
 * Removing a block, or every block of a shift with a break, behind one confirmation. The backend
 * removes one block per call, so a whole shift is removed block by block; a refusal part way
 * stops there and says why.
 */
export function RemoveShiftControls({ selected, blocks, now, onRemoved }: RemoveShiftControlsProps) {
  const text = shiftEditText;
  const [asked, setAsked] = useState<Shift[] | null>(null);
  const [message, setMessage] = useState<string>();
  const [pending, startTransition] = useTransition();
  const notStarted = (block: Shift) => Date.parse(block.starts_at) > now;

  function remove(targets: Shift[]) {
    setMessage(undefined);
    startTransition(async () => {
      const conflicts: TimeOffConflict[] = [];

      try {
        for (const block of targets) {
          const result = await removeShiftAction({ id: block.id, version: block.version });

          if (!result.ok) {
            setMessage(result.error.code === 'version_conflict' ? formMessages.conflict : result.error.message);
            setAsked(null);
            return;
          }

          conflicts.push(...result.data.conflicts);
        }
      } catch {
        setMessage(formMessages.unreachable);
        setAsked(null);
        return;
      }

      onRemoved(conflicts);
    });
  }

  if (asked) {
    return (
      <div className="space-y-3 rounded-lg border border-red-200 bg-red-50/60 p-3">
        <p className="text-sm text-red-900">{text.removeNote}</p>
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" disabled={pending} onClick={() => setAsked(null)}>
            {text.keep}
          </Button>
          <Button variant="danger" disabled={pending} onClick={() => remove(asked)}>
            {pending ? text.removing : text.confirm}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-wrap items-center justify-end gap-2 border-t border-ink-100 pt-4">
      <FormStatus message={message} savedText="" />
      <Button variant="danger" size="sm" disabled={!notStarted(selected)} onClick={() => setAsked([selected])}>
        {blocks.length > 1 ? text.remove : text.removeSingle}
      </Button>
      {blocks.length > 1 && (
        <Button variant="danger" size="sm" disabled={!blocks.every(notStarted)} onClick={() => setAsked(blocks)}>
          {text.removeAll(blocks.length)}
        </Button>
      )}
    </div>
  );
}
