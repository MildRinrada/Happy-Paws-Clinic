import { tolerant } from '@/features/analytics/lib/tolerant-read';
import type { CursorPage } from '@/types/pagination';
import { listLabRequests } from '../api/lab-requests';
import type { LabRequest } from '../types/lab.types';
import type { LabFilters } from './lab-filters';
import { LAB_PAGE_SIZE } from './load-lab-worklist';

/** The two tabs that count what is waiting. */
export type CountedTab = 'pending' | 'resulted';

/** A tab's number: `more` when the read stopped at its page size, so it is drawn "100+". */
export interface TabCount {
  count: number;
  more: boolean;
}

export type LabTabCounts = Partial<Record<CountedTab, TabCount>>;

const COUNTED: CountedTab[] = ['pending', 'resulted'];

const countOf = (page: CursorPage<LabRequest>): TabCount => ({ count: page.data.length, more: Boolean(page.page.next_cursor) });

/**
 * The numbers on "รอผล" and "รออ่านผล": one page of 100 per tab, with the same "mine" and branch as
 * the list, but not the search or destination (those narrow the page on screen, not the work).
 * The tab on screen reuses the page already read. A count that cannot be read is left out, and
 * the tab shows no number rather than a wrong one.
 */
export async function loadLabTabCounts(filters: LabFilters, branchId: string, shown: CursorPage<LabRequest>): Promise<LabTabCounts> {
  const answers = await Promise.all(
    COUNTED.map(async (tab) => {
      if (tab === filters.tab && !filters.cursor) return [tab, countOf(shown)] as const;

      const page = await tolerant(() =>
        listLabRequests({ status: tab, requestedBy: filters.mine ? 'me' : undefined, branchId, limit: LAB_PAGE_SIZE }),
      );
      return [tab, page ? countOf(page) : undefined] as const;
    }),
  );

  const counts: LabTabCounts = {};
  for (const [tab, count] of answers) {
    if (count) counts[tab] = count;
  }

  return counts;
}
