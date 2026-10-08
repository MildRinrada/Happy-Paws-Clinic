'use client';

import { Field, Select } from '@/components/ui/field';
import { movementPage } from '../content/movement-page';
import type { StockLot } from '../types/inventory.types';

interface Props {
  id: string;
  label: string;
  lots: StockLot[];
  value: string;
  onChange: (lotId: string) => void;
  /** What choosing no lot means here: first expiry first, or "the only lot". */
  emptyLabel: string;
  error?: string;
}

/** One lot of the item at the branch. Expired lots are listed and marked, so they can be issued off the shelf. */
export function LotSelect({ id, label, lots, value, onChange, emptyLabel, error }: Props) {
  return (
    <Field label={label} htmlFor={id} error={error}>
      <Select id={id} value={value} onChange={(event) => onChange(event.target.value)} aria-invalid={Boolean(error)}>
        <option value="">{emptyLabel}</option>
        {lots.map((lot) => (
          <option key={lot.id} value={lot.id}>
            {movementPage.lotOption(lot.lot_number, lot.quantity_on_hand, lot.expired)}
          </option>
        ))}
      </Select>
    </Field>
  );
}
