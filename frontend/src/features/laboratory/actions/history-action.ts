'use server';

import { z } from 'zod';
import { runAction } from '@/lib/actions/action-result';
import { listLabRequests } from '../api/lab-requests';
import type { PastRequest } from '../lib/previous-values';

const input = z.object({ petId: z.string().min(1).max(64) });

/**
 * One pet's lab history for the "ครั้งก่อน" column: its latest 100 requests, cut down to what the
 * column compares. Asked for once per pet, when one of its rows with results is first opened, and
 * kept in the browser after that; the read is written to the access log as a look at the pet's
 * results (`pet.lab_results_viewed`), which it is.
 */
export async function petLabHistoryAction(request: z.input<typeof input>) {
  const { petId } = input.parse(request);

  return runAction(async (): Promise<PastRequest[]> => {
    const page = await listLabRequests({ petId, limit: 100 });

    return page.data.map((past) => ({
      id: past.id,
      requested_at: past.requested_at,
      results: past.results.map(({ analyte, value_numeric, value_text, unit, voided }) => ({ analyte, value_numeric, value_text, unit, voided })),
    }));
  });
}
