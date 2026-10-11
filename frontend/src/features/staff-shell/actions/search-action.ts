'use server';

import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { searchConsole } from '../lib/search-console';
import type { ConsoleSearchResults } from '../types/staff-shell.types';

const query = z.object({
  q: z.string().trim().max(100),
  /** Whether to look up patients too. A hint for the UI only: the backend checks `patient:read` itself. */
  pets: z.boolean(),
});

/**
 * The top bar's search, run on the server so the staff token never reaches the browser. Fewer than
 * two letters answer nothing rather than the whole register.
 */
export async function searchConsoleAction(input: { q: string; pets: boolean }): Promise<ActionResult<ConsoleSearchResults>> {
  const { q, pets } = query.parse(input);

  if (q.length < 2) {
    return { ok: true, data: { customers: [], pets: [] } };
  }

  return runAction(() => searchConsole(q, pets));
}
