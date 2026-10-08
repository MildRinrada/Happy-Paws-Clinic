import type { ItemsQuery, StockStatus } from '../types/inventory.types';

const STATUSES: StockStatus[] = ['ok', 'low', 'out'];

function first(value: string | string[] | undefined): string | undefined {
  const single = Array.isArray(value) ? value[0] : value;
  const trimmed = single?.trim();

  return trimmed ? trimmed : undefined;
}

/** The list's filters from the address. An unknown status is dropped rather than sent on to be refused. */
export function readItemsQuery(params: Record<string, string | string[] | undefined>): ItemsQuery {
  const status = first(params.status);

  return {
    q: first(params.q)?.slice(0, 100),
    status: status && (STATUSES as string[]).includes(status) ? status : undefined,
  };
}
