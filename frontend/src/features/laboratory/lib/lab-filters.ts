import type { LabStatus } from '../types/lab.types';

/** Where the worklist lives in the console. */
export const LAB_PATH = '/lab';

export type LabTab = LabStatus | 'all';

export const LAB_TABS: readonly LabTab[] = ['pending', 'resulted', 'reviewed', 'all'];

export interface LabFilters {
  tab: LabTab;
  /** Only the caller's own requests: "งานค้างของหมอ". */
  mine: boolean;
  cursor?: string;
}

const isTab = (value: string | undefined): value is LabTab => LAB_TABS.includes(value as LabTab);

/**
 * Reads the worklist's filters from the address bar. With no tab chosen, the lab sees what waits
 * on the lab and a vet sees what waits on a vet.
 */
export function parseLabFilters(params: Record<string, string | undefined>, canWriteResults: boolean): LabFilters {
  return {
    tab: isTab(params.status) ? params.status : canWriteResults ? 'pending' : 'resulted',
    mine: params.mine === '1',
    cursor: params.cursor || undefined,
  };
}

/** The address of a worklist view. Changing a filter starts again from the first page. */
export function labHref(filters: LabFilters): string {
  const query = new URLSearchParams({ status: filters.tab });
  if (filters.mine) query.set('mine', '1');
  if (filters.cursor) query.set('cursor', filters.cursor);

  return `${LAB_PATH}?${query.toString()}`;
}
