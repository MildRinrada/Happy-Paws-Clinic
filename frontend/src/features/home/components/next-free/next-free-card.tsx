'use client';

import { useEffect, useState } from 'react';
import { SiteCard } from '@/components/site/site-card';
import { pickNextFree } from '../../lib/pick-next-free';
import type { NextFreeTimes } from '../../types/next-free-time.types';
import { NextFreeFull } from './next-free-full';
import { NextFreeRow } from './next-free-row';

/** How often an open page checks its times against the clock, in milliseconds. */
const RECHECK_EVERY = 30_000;

interface NextFreeCardProps {
  data: NextFreeTimes;
  /** When the server drew the page, so the first draw in the browser matches it exactly. */
  renderedAt: number;
}

/**
 * The white card in the hero. Drawn first with the server's clock, then kept against the
 * browser's: a page left open past a time drops it and moves on to the next one.
 */
export function NextFreeCard({ data, renderedAt }: NextFreeCardProps) {
  const [now, setNow] = useState(renderedAt);

  useEffect(() => {
    setNow(Date.now());
    const timer = window.setInterval(() => setNow(Date.now()), RECHECK_EVERY);
    return () => window.clearInterval(timer);
  }, []);

  const slots = pickNextFree(data, now);

  return (
    <SiteCard tone="raised" padding="none" className="max-w-[520px] px-4 py-4 text-ink-900 sm:px-5">
      {slots.length === 0 ? (
        <NextFreeFull serviceId={data.serviceId} />
      ) : (
        <ul className="divide-y divide-ink-100">
          {slots.map((slot) => (
            <NextFreeRow key={slot.branchId} slot={slot} serviceId={data.serviceId} serviceName={data.serviceName} now={now} />
          ))}
        </ul>
      )}
    </SiteCard>
  );
}
