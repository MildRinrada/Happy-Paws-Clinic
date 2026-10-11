import type { LabDestination, LabStatus } from '../types/lab.types';

/** Where the worklist lives in the console. */
export const LAB_PATH = '/lab';

export type LabTab = LabStatus | 'all';

export const LAB_TABS: readonly LabTab[] = ['pending', 'resulted', 'reviewed', 'all'];

export const LAB_DESTINATIONS: readonly LabDestination[] = ['in_house', 'external'];

export interface LabFilters {
  tab: LabTab;
  /** Only the caller's own requests: "งานค้างของหมอ". */
  mine: boolean;
  /** In-house or sent out; all when absent. */
  destination?: LabDestination;
  /** A pet's name or HN, matched within the page loaded. */
  query?: string;
  cursor?: string;
}

const isTab = (value: string | undefined): value is LabTab => LAB_TABS.includes(value as LabTab);
const isDestination = (value: string | undefined): value is LabDestination => LAB_DESTINATIONS.includes(value as LabDestination);

/**
 * Reads the worklist's filters from the address bar, so a filtered view can be bookmarked and the
 * back button works. With no tab chosen, the lab sees what waits on the lab and a vet sees what
 * waits on a vet.
 */
export function parseLabFilters(params: Record<string, string | undefined>, canWriteResults: boolean): LabFilters {
  const query = params.q?.trim().slice(0, 100);

  return {
    tab: isTab(params.status) ? params.status : canWriteResults ? 'pending' : 'resulted',
    mine: params.mine === '1',
    destination: isDestination(params.dest) ? params.dest : undefined,
    query: query || undefined,
    cursor: params.cursor || undefined,
  };
}

/** The address of a worklist view. Changing a filter starts again from the first page. */
export function labHref(filters: LabFilters): string {
  const query = new URLSearchParams({ status: filters.tab });
  if (filters.mine) query.set('mine', '1');
  if (filters.destination) query.set('dest', filters.destination);
  if (filters.query) query.set('q', filters.query);
  if (filters.cursor) query.set('cursor', filters.cursor);

  return `${LAB_PATH}?${query.toString()}`;
}
