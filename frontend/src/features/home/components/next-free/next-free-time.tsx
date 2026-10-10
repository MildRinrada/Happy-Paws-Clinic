import { loadNextFreeTimes } from '../../lib/load-next-free-times';
import { NextFreeCard } from './next-free-card';

/**
 * "ว่างเร็วสุดวันนี้ 14:30" under the hero's buttons. The times are read on the server and shared
 * by every visitor for a minute (`load-next-free-times.ts`), so the availability endpoint is not
 * asked once per visitor. Viewing it holds nothing. When the API is away, or the clinic offers
 * nothing online, there is no block at all.
 */
export async function NextFreeTime() {
  const data = await loadNextFreeTimes();

  if (!data) return null;

  return (
    <div className="mt-6">
      <NextFreeCard data={data} renderedAt={Date.now()} />
    </div>
  );
}
