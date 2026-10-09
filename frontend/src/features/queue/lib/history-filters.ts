import { clinicDay } from '@/lib/format/datetime';
import type { HistoryFilter, TicketStatus } from '../types/queue.types';

const FILTERS: Record<HistoryFilter, readonly TicketStatus[] | null> = {
  finished: ['served', 'left'],
  served: ['served'],
  left: ['left'],
  all: null,
};

export interface HistoryQuery {
  /** `YYYY-MM-DD`, clinic time. */
  date: string;
  filter: HistoryFilter;
}

const DAY = /^\d{4}-\d{2}-\d{2}$/;

/**
 * The history view's query string, read defensively: it is typed by hand into an address bar as
 * often as it is set by the form. A date that is not a real day falls back to today rather than
 * becoming a `400` from the backend.
 */
export function readHistoryQuery(params: Record<string, string | string[] | undefined>): HistoryQuery {
  const date = typeof params.date === 'string' ? params.date : '';
  const status = typeof params.status === 'string' ? params.status : '';

  return {
    date: DAY.test(date) && !Number.isNaN(Date.parse(date)) ? date : clinicDay(new Date()),
    filter: Object.hasOwn(FILTERS, status) ? (status as HistoryFilter) : 'finished',
  };
}

/** Whether a ticket belongs in the view. The backend has no status filter, so this is the page's. */
export function matchesFilter(status: TicketStatus, filter: HistoryFilter): boolean {
  const statuses = FILTERS[filter];

  return statuses === null || statuses.includes(status);
}

export const HISTORY_FILTERS = Object.keys(FILTERS) as HistoryFilter[];
