import { staffApi } from '@/lib/api/server-client';
import type { LineFollowerPage } from '../types/line.types';

/**
 * `GET /staff/integrations/line/followers` (customer:write), a contract extension
 * (docs/contract-extensions.md § 18): who followed or wrote to the clinic's LINE account in the
 * last 14 days and is on no customer's record yet, most recent first.
 */
export function listLineFollowers(query: { q?: string; cursor?: string } = {}): Promise<LineFollowerPage> {
  return staffApi().get<LineFollowerPage>('/staff/integrations/line/followers', {
    query: { unlinked: true, q: query.q || undefined, cursor: query.cursor, limit: 20 },
  });
}
