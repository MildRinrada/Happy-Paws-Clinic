import { unstable_cache } from 'next/cache';
import { listBranches } from '@/features/clinic/api/listBranches';
import { getWaitEstimate } from '../api/getWaitEstimate';
import { waitEstimate } from '../content/wait-estimate';
import type { BranchWait, WaitEstimates } from '../types/wait-estimate.types';

/** How long one read is reused for every visitor, in seconds: the same as the API's own cache. */
const FRESH_FOR = 30;

/**
 * The oldest read still shown, in milliseconds. After `FRESH_FOR` the cache hands out the last read
 * while it reads again in the background; if the API is away that read fails and the old one stays.
 * Past this age it is not shown, so a stopped API hides the block within a couple of minutes.
 */
const MAX_AGE = 3 * FRESH_FOR * 1000;

/**
 * Reads the open branches and each one's estimate. Throws when the branches cannot be read or no
 * branch answered, so a failure is not cached and the block is hidden.
 */
async function readWaitEstimates(): Promise<WaitEstimates> {
  const branches = (await listBranches()).slice(0, waitEstimate.maxBranches);

  const read = await Promise.allSettled(
    branches.map(async (branch): Promise<BranchWait> => ({
      branchId: branch.id,
      branchName: branches.length > 1 ? branch.name : undefined,
      estimate: await getWaitEstimate(branch.id),
    })),
  );

  const answered = read.flatMap((result) => (result.status === 'fulfilled' ? [result.value] : []));

  if (answered.length === 0) {
    throw new Error('No branch answered GET /queue/wait-estimate');
  }

  return { branches: answered, readAt: Date.now() };
}

/** Shared by every visitor for 30 seconds. A read that throws is not kept. */
const cachedWaitEstimates = unstable_cache(readWaitEstimates, ['wait-estimates'], { revalidate: FRESH_FOR });

/**
 * Each open branch's walk-in wait, or `null` when there is nothing to show: the API is away, or the
 * last good read is too old. Never throws: the block is extra, and every page renders without it.
 */
export async function loadWaitEstimates(now: Date = new Date()): Promise<WaitEstimates | null> {
  try {
    const data = await cachedWaitEstimates();
    return now.getTime() - data.readAt <= MAX_AGE ? data : null;
  } catch {
    return null;
  }
}
