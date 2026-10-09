import { staffApi } from '@/lib/api/server-client';
import type { NewSegmentInput, Segment } from '../types/crm.types';

/**
 * `POST /staff/segments`. Needs `crm:manage`. There is no endpoint to list or read a segment, so
 * the screen shows the answer right away and carries the id forward in a link.
 */
export function createSegment(input: NewSegmentInput): Promise<Segment> {
  return staffApi().post<Segment>('/staff/segments', { body: input });
}
