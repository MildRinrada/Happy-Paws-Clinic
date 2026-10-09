import type { InvoiceLineKind } from '../types/billing.types';

/** The `value` prefix of a service entry. What follows it is the service id. */
export const SERVICE_ENTRY_PREFIX = 'service:';

/** The service id behind a picked entry, or undefined when the line was not picked from the services. */
export function serviceIdOf(value: string): string | undefined {
  return value.startsWith(SERVICE_ENTRY_PREFIX) ? value.slice(SERVICE_ENTRY_PREFIX.length) || undefined : undefined;
}

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
