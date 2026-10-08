'use client';

import { Field, Select } from '@/components/ui/field';
import { createInvoicePage } from '../../content/create-invoice-page';
import type { CatalogueEntry } from '../../lib/catalogue-entry';

interface Props {
  id: string;
  catalogue: CatalogueEntry[];
  value: string;
  onChange: (entry: CatalogueEntry | null) => void;
}

/** Fill a line from a service or a stock item, or leave it to be typed by hand. */
export function CatalogueSelect({ id, catalogue, value, onChange }: Props) {
  const text = createInvoicePage.lines;
  const services = catalogue.filter((entry) => entry.group === 'service');
  const items = catalogue.filter((entry) => entry.group === 'item');

  return (
    <Field label={text.pick} htmlFor={id}>
      <Select id={id} value={value} onChange={(event) => onChange(catalogue.find((entry) => entry.value === event.target.value) ?? null)}>
        <option value="">{text.pickNone}</option>
        {services.length > 0 && (
          <optgroup label={text.services}>
            {services.map((entry) => (
              <option key={entry.value} value={entry.value}>
                {entry.label}
              </option>
            ))}
          </optgroup>
        )}
        {items.length > 0 && (
          <optgroup label={text.items}>
            {items.map((entry) => (
              <option key={entry.value} value={entry.value}>
                {entry.label}
              </option>
            ))}
          </optgroup>
        )}
      </Select>
    </Field>
  );
}
