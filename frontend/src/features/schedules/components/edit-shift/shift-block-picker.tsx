'use client';

import { shiftEditText } from '../../content/shift-edit';
import { blockHours } from '../../lib/chip-label';
import type { Shift } from '../../types/schedule.types';

interface ShiftBlockPickerProps {
  blocks: Shift[];
  selectedId: string;
  /** Now, in milliseconds: a block already over cannot be picked. */
  now: number;
  onPick: (block: Shift) => void;
}

/**
 * Which block of a shift with a break to change or remove. The backend keeps the time before and
 * after the break as two blocks, and every change acts on one of them.
 */
export function ShiftBlockPicker({ blocks, selectedId, now, onPick }: ShiftBlockPickerProps) {
  const text = shiftEditText;

  return (
    <fieldset className="space-y-2">
      <legend className="text-sm text-ink-700">{text.blocks}</legend>
      <div className="flex flex-wrap gap-2">
        {blocks.map((block, index) => {
          const selected = block.id === selectedId;

          return (
            <button
              key={block.id}
              type="button"
              aria-pressed={selected}
              disabled={Date.parse(block.ends_at) <= now}
              className={`rounded-lg border px-3 py-1.5 text-sm tabular-nums disabled:cursor-not-allowed disabled:opacity-40 ${
                selected ? 'border-brand-500 bg-brand-50 font-medium text-brand-800' : 'border-ink-200 text-ink-700 hover:bg-ink-50'
              }`}
              onClick={() => onPick(block)}
            >
              {text.block(index + 1, blockHours(block))}
            </button>
          );
        })}
      </div>
    </fieldset>
  );
}
