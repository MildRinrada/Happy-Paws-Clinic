import { unstable_cache } from 'next/cache';
import { getAvailability } from '@/features/booking/api/getAvailability';
import { addDays } from '@/features/booking/lib/booking-dates';
import { listBranches } from '@/features/clinic/api/listBranches';
import { listServices } from '@/features/service-catalog/api/listServices';
import { clinicDay } from '@/lib/format/datetime';
import { nextFreeTime } from '../content/next-free-time';
import type { BranchFreeTimes, NextFreeTimes } from '../types/next-free-time.types';
import { featuredService } from './featured-service';

/** How long one read is reused for every visitor, in seconds. */
const FRESH_FOR = 60;

/**
 * The oldest read still shown, in milliseconds. After `FRESH_FOR` the cache hands out the last read
 * while it reads again in the background; if the API is away that read fails and the old one
 * stays. Past this age it is not shown, so a stopped API hides the block within a few minutes.
 */
const MAX_AGE = 3 * FRESH_FOR * 1000;

/** Times kept per branch and day: enough that some are still ahead when the cached read is a minute old. */
const KEEP = 8;

/** One day's distinct free start times, earliest first. The API repeats a time once per free vet. */
async function freeTimes(serviceId: string, day: string, branchId: string): Promise<string[]> {
  const { slots } = await getAvailability(serviceId, day, branchId);
  const times = [...new Set(slots.map((slot) => slot.starts_at))];

  return times.sort((a, b) => Date.parse(a) - Date.parse(b)).slice(0, KEEP);
}

/**
 * Reads, for a clinic day: the public menu, the open branches, and the featured service's free
 * times at each branch that offers it, that day and the next. Reads only: nothing is held.
 *
 * Throws when the menu or the branches cannot be read, or when no branch's times could be, so the
 * caller can tell "the API is away" (hide the block) from "nothing free" (say so). `null` when the
 * clinic offers nothing online at an open branch.
 */
async function readNextFreeTimes(today: string): Promise<NextFreeTimes | null> {
  const [services, branches] = await Promise.all([listServices(), listBranches()]);
  const service = featuredService(services);

  if (!service) return null;

  // An empty `branch_ids` means every branch offers it, as the billing and CRM pickers read it.
  const offering = branches
    .filter((branch) => service.branch_ids.length === 0 || service.branch_ids.includes(branch.id))
    .slice(0, nextFreeTime.maxBranches);
  const tomorrow = addDays(today, 1);

  if (offering.length === 0) return null;

  const read = await Promise.allSettled(
    offering.map(async (branch): Promise<BranchFreeTimes> => {
      const [first, second] = await Promise.all([
        freeTimes(service.id, today, branch.id),
        freeTimes(service.id, tomorrow, branch.id),
      ]);

      return { branchId: branch.id, branchName: branches.length > 1 ? branch.name : undefined, times: [...first, ...second] };
    }),
  );

  const answered = read.flatMap((result) => (result.status === 'fulfilled' ? [result.value] : []));

  if (answered.length === 0) {
    throw new Error('No branch answered GET /booking/availability');
  }

  return { serviceId: service.id, serviceName: service.name, branches: answered, readAt: Date.now() };
}

/**
 * Shared by every visitor for a minute, keyed by the clinic day so a new day starts a new read.
 * A read that throws is not kept, so the next visitor tries again.
 */
const cachedNextFreeTimes = unstable_cache(readNextFreeTimes, ['home-next-free-times'], { revalidate: FRESH_FOR });

/**
 * The featured service's free times for the home page, or `null` when there is nothing to show:
 * the API is away (or the last good read is too old), or the clinic offers nothing online. Never
 * throws: the block is extra, and the home page renders without it.
 */
export async function loadNextFreeTimes(now: Date = new Date()): Promise<NextFreeTimes | null> {
  try {
    const data = await cachedNextFreeTimes(clinicDay(now));
    return data && now.getTime() - data.readAt <= MAX_AGE ? data : null;
  } catch {
    return null;
  }
}
