import type { DrugOption } from '@/features/prescriptions/types/drug-option.types';
import { drugPicker } from '../../content/drug-picker';

/**
 * Says under the drug field what the dispense will do: a drug picked from stock is taken off the
 * shelf and charged; a typed name is not tied to stock.
 */
export function DrugLinkNote({ id, name, item }: { id: string; name: string; item?: DrugOption }) {
  if (item) {
    return (
      <p id={id} className="text-sm text-emerald-800">
        {drugPicker.linked(item.name, item.unit, item.unit_price_satang)}
      </p>
    );
  }

  return (
    <p id={id} className={name.trim() === '' ? 'text-sm text-ink-500' : 'text-sm text-amber-800'}>
      {name.trim() === '' ? drugPicker.hintEmpty : drugPicker.freeText}
    </p>
  );
}
