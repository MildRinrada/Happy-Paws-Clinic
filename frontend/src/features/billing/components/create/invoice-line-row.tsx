'use client';

import { useId } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select } from '@/components/ui/field';
import { createInvoicePage } from '../../content/create-invoice-page';
import { lineKindLabel, lineKinds } from '../../content/line-kinds';
import type { CatalogueEntry } from '../../lib/catalogue-entry';
import type { InvoiceLineDraft } from '../../lib/invoice-line-draft';
import type { InvoiceLineKind } from '../../types/billing.types';
import { CatalogueSelect } from './catalogue-select';

interface Props {
  index: number;
  line: InvoiceLineDraft;
  catalogue: CatalogueEntry[];
  /** Refusals for this line, with the `lines[n].` prefix taken off. */
  errors: Record<string, string>;
  onChange: (line: InvoiceLineDraft) => void;
  onRemove?: () => void;
}

/** One line of a hand-raised bill: picked from the catalogue or typed, then quantity and price. */
export function InvoiceLineRow({ index, line, catalogue, errors, onChange, onRemove }: Props) {
  const text = createInvoicePage.lines;
  // useId, not the row's random key: the first row is drawn on the server too.
  const id = `invoice-line-${useId()}`;
  const set = (patch: Partial<InvoiceLineDraft>) => onChange({ ...line, ...patch });

  function pick(entry: CatalogueEntry | null) {
    if (!entry) {
      set({ source: '' });
      return;
    }

    set({
      source: entry.value,
      description: entry.description,
      kind: entry.kind,
      unitPrice: (entry.unitPriceSatang / 100).toFixed(2),
    });
  }

  return (
    <fieldset className="space-y-3 rounded-lg border border-ink-200 p-3">
      <div className="flex items-center justify-between">
        <legend className="text-sm font-medium text-ink-800">{text.lineHeading(index + 1)}</legend>
        {onRemove && (
          <Button size="sm" variant="ghost" onClick={onRemove}>
            {text.remove}
          </Button>
        )}
      </div>

      {catalogue.length > 0 && <CatalogueSelect id={`${id}-source`} catalogue={catalogue} value={line.source} onChange={pick} />}

      <div className="grid grid-cols-2 gap-3 sm:grid-cols-[1fr_7rem_5rem_8rem]">
        <div className="col-span-2 sm:col-span-1">
          <Field label={text.description} htmlFor={`${id}-description`} error={errors.description}>
            <Input
              id={`${id}-description`}
              value={line.description}
              maxLength={300}
              onChange={(event) => set({ description: event.target.value })}
            />
          </Field>
        </div>
        <Field label={text.kind} htmlFor={`${id}-kind`} error={errors.kind}>
          <Select id={`${id}-kind`} value={line.kind} onChange={(event) => set({ kind: event.target.value as InvoiceLineKind })}>
            {lineKinds.map((kind) => (
              <option key={kind} value={kind}>
                {lineKindLabel[kind]}
              </option>
            ))}
          </Select>
        </Field>
        <Field label={text.quantity} htmlFor={`${id}-quantity`} error={errors.quantity}>
          <Input
            id={`${id}-quantity`}
            type="number"
            min={1}
            step={1}
            value={line.quantity}
            onChange={(event) => set({ quantity: event.target.value })}
          />
        </Field>
        <div className="col-span-2 sm:col-span-1">
          <Field label={text.unitPrice} htmlFor={`${id}-price`} error={errors.unit_price_satang}>
            <Input id={`${id}-price`} inputMode="decimal" value={line.unitPrice} onChange={(event) => set({ unitPrice: event.target.value })} />
          </Field>
        </div>
      </div>

      {line.kind === 'medicine' && <p className="text-xs text-ink-500">{text.medicineNote}</p>}
    </fieldset>
  );
}
