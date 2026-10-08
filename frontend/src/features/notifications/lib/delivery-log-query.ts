import type { DeliveryLogQuery } from '../types/notification.types';

/** The log page's address, and the filters read back out of it. */
export const LOG_PATH = '/notifications/log';

const KEYS = ['status', 'channel', 'source', 'customer_id', 'template', 'batch_id', 'cursor'] as const;

/** Keeps the known filters, trimmed and bounded; anything else in the address is ignored. */
export function readLogQuery(params: Record<string, string | string[] | undefined>): DeliveryLogQuery {
  const query: DeliveryLogQuery = {};

  for (const key of KEYS) {
    const raw = params[key];
    const value = (Array.isArray(raw) ? raw[0] : raw)?.trim().slice(0, 200);

    if (value) {
      query[key] = value;
    }
  }

  return query;
}

/** The same filters on the next page. */
export function nextPageHref(query: DeliveryLogQuery, cursor: string): string {
  const params = new URLSearchParams();

  for (const [key, value] of Object.entries(query)) {
    if (value && key !== 'cursor') params.set(key, value);
  }

  params.set('cursor', cursor);
  return `${LOG_PATH}?${params}`;
}
