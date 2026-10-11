import { useId } from 'react';
import { SiteCard } from '@/components/site/site-card';
import { waitEstimate } from '../content/wait-estimate';
import type { WaitEstimates } from '../types/wait-estimate.types';
import { WalkInWaitRow } from './walk-in-wait-row';

/**
 * On the teal home hero, under the white "ว่างเร็วสุด" card: see-through, so the two do not compete.
 * On a white page it is a `SiteCard` like the other boxes there.
 */
const onHero = 'rounded-2xl bg-white/10 px-4 py-3 text-white ring-1 ring-white/25 sm:px-5';

interface WalkInWaitCardProps {
  data: WaitEstimates;
  tone: 'hero' | 'plain';
  now: Date;
}

/** The heading and one line per branch. The page decides the width; the card fills it. */
export function WalkInWaitCard({ data, tone, now }: WalkInWaitCardProps) {
  const headingId = useId();
  const onDark = tone === 'hero';

  const body = (
    <>
      <h2 id={headingId} className={`text-sm font-semibold ${onDark ? 'text-white/85' : 'text-site-muted'}`}>
        {waitEstimate.heading}
      </h2>
      <ul className="mt-1.5 space-y-1.5">
        {data.branches.map((wait) => (
          <WalkInWaitRow key={wait.branchId} wait={wait} now={now} onDark={onDark} />
        ))}
      </ul>
      {!onDark && <p className="mt-3 text-sm text-site-muted">{waitEstimate.note}</p>}
    </>
  );

  if (onDark) {
    return (
      <section aria-labelledby={headingId} className={onHero}>
        {body}
      </section>
    );
  }

  return (
    <SiteCard as="section" tone="whiteSoft" ariaLabelledBy={headingId} className="text-ink-900">
      {body}
    </SiteCard>
  );
}
