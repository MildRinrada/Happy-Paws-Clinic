import type { DrugOption } from '@/features/prescriptions/types/drug-option.types';
import { drugPicker } from '../../content/drug-picker';
import type { DrugSearchState } from '../../hooks/use-drug-options';
import { DrugOptionRow } from './drug-option-row';

interface DrugSuggestionsProps {
  id: string;
  options: DrugOption[];
  state: DrugSearchState;
  active: number;
  optionId: (index: number) => string;
  onPick: (option: DrugOption) => void;
}

/**
 * The listbox under the drug field. Picked with the mouse on press, not release, so the field keeps
 * focus and the list does not close before the pick lands.
 */
export function DrugSuggestions({ id, options, state, active, optionId, onPick }: DrugSuggestionsProps) {
  const status =
    state === 'searching' ? drugPicker.searching
    : state === 'failed' ? drugPicker.failed
    : state === 'done' && options.length === 0 ? drugPicker.noMatch
    : undefined;

  return (
    <div className="absolute inset-x-0 top-full z-20 mt-1 rounded-lg border border-ink-200 bg-white shadow-lg">
      {status && <p className="px-3 py-2 text-sm text-ink-500">{status}</p>}
      {options.length > 0 && (
        <ul id={id} role="listbox" aria-label={drugPicker.listLabel} className="max-h-72 overflow-y-auto py-1">
          {options.map((option, index) => (
            <li
              key={option.item_id}
              id={optionId(index)}
              role="option"
              aria-selected={index === active}
              className={`cursor-pointer px-3 py-2 text-sm ${index === active ? 'bg-brand-50' : 'hover:bg-ink-50'}`}
              onMouseDown={(event) => {
                event.preventDefault();
                onPick(option);
              }}
            >
              <DrugOptionRow option={option} />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
