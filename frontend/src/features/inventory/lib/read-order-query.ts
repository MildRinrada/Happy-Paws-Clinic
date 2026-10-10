import type { PurchaseOrderStatus } from '../types/inventory.types';

const STATUSES: PurchaseOrderStatus[] = ['ordered', 'partially_received', 'received', 'cancelled'];

function text(params: Record<string, string | string[] | undefined>, key: string): string | undefined {
  const value = params[key];
  return typeof value === 'string' && value.trim() ? value.trim() : undefined;
}

/** `?supplier=&status=&cursor=` of the order list. An unknown status is dropped rather than sent. */
export function readOrderQuery(params: Record<string, string | string[] | undefined>) {
  const status = text(params, 'status');

  return {
    supplier: text(params, 'supplier'),
    status: STATUSES.find((value) => value === status),
    cursor: text(params, 'cursor'),
  };
}
