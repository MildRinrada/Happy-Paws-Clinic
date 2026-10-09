import { staffApi } from '@/lib/api/server-client';
import type { CursorPage } from '@/types/pagination';
import type { Segment } from '../types/crm.types';

/**
 * `GET /staff/segments`. Needs `crm:read`. Newest first, with the counts taken when each was saved.
 * docs/contract-extensions.md § 25.
 */
export function listSegments(cursor?: string): Promise<CursorPage<Segment>> {
  return staffApi().get<CursorPage<Segment>>('/staff/segments', { query: { cursor, limit: 25 } });
}

/** `GET /staff/segments/{id}`. Needs `crm:read`. Counted again now. 403 for another branch's group. § 25. */
export function getSegment(id: string): Promise<Segment> {
  return staffApi().get<Segment>(`/staff/segments/${encodeURIComponent(id)}`);
}
