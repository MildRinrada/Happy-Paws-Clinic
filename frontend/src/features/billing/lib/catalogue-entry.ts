import type { InvoiceLineKind } from '../types/billing.types';

/**
 * Something the counter can ring up without typing it: a service, or an item from the stock room.
 * Picking one fills a line's description, kind and price; all three stay editable.
 */
export interface CatalogueEntry {
  /** `service:<id>` or `item:<id>`, unique across both groups. */
  value: string;
  group: 'service' | 'item';
  /** What the picker shows, price and stock included. */
  label: string;
  description: string;
  kind: InvoiceLineKind;
  unitPriceSatang: number;
}
