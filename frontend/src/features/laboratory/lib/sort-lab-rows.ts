import type { LabRequest } from '../types/lab.types';
import type { LabTab } from './lab-filters';

/** The instant a waiting request started waiting, for ordering: sent for `pending`, latest results for `resulted`. */
const waitingSince = (request: LabRequest): number =>
  Date.parse(request.status === 'resulted' ? request.resulted_at ?? request.requested_at : request.requested_at);

/**
 * The order of the two waiting tabs: urgent first, then whoever has waited longest. The read and
 * "all" tabs keep the API's order, newest first, because nothing in them is waiting.
 *
 * This sorts the page that was loaded (up to 100 requests), not the whole list: the API pages
 * newest first and has no other order. A clinic with more than 100 requests waiting on one tab
 * sees the oldest beyond the first page only by following "รายการที่เก่ากว่านี้".
 */
export function sortLabRows<T extends { request: LabRequest }>(rows: T[], tab: LabTab): T[] {
  if (tab !== 'pending' && tab !== 'resulted') return rows;

  return [...rows].sort((first, second) => {
    const urgency = Number(second.request.priority === 'urgent') - Number(first.request.priority === 'urgent');
    return urgency !== 0 ? urgency : waitingSince(first.request) - waitingSince(second.request);
  });
}
