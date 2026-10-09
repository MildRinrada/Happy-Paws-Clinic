import type { BroadcastSegment } from '../types/line.types';

type Query = Record<string, string | string[] | undefined>;

const single = (value: string | string[] | undefined) => (typeof value === 'string' ? value.trim() : '');

/**
 * The segment the broadcast page was opened for, from the link `/crm/segments` hands out:
 * `?segment_id=&segment_name=&reachable=`. Only the id is needed to send; the name and the count
 * are for the screen, so a missing name falls back to the id and a bad count is left out.
 */
export function readBroadcastSegment(query: Query): BroadcastSegment | undefined {
  const id = single(query.segment_id);
  if (!id) return undefined;

  const reachable = Number(single(query.reachable));

  return {
    id,
    name: single(query.segment_name).slice(0, 120) || id,
    reachable: single(query.reachable) && Number.isInteger(reachable) && reachable >= 0 ? reachable : undefined,
  };
}
