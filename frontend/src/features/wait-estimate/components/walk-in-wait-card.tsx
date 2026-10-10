import { useId } from 'react';
import { waitEstimate } from '../content/wait-estimate';
import type { WaitEstimates } from '../types/wait-estimate.types';
import { WalkInWaitRow } from './walk-in-wait-row';

const tones = {
  // On the teal home hero, under the white "ว่างเร็วสุด" card: lighter, so the two do not compete.
  hero: 'rounded-2xl bg-white/10 px-4 py-3 text-white ring-1 ring-white/25 sm:px-5',
  // On a white page, like the other cards there.
  plain: 'rounded-2xl border border-site-border/60 bg-white p-5 text-ink-900',
};

interface WalkInWaitCardProps {
  data: WaitEstimates;
  tone: keyof typeof tones;
  now: Date;
}

/** The heading and one line per branch. The page decides the width; the card fills it. */
export function WalkInWaitCard({ data, tone, now }: WalkInWaitCardProps) {
  const headingId = useId();
  const onDark = tone === 'hero';

  return (
    <section aria-labelledby={headingId} className={tones[tone]}>
      <h2 id={headingId} className={`text-sm font-semibold ${onDark ? 'text-white/85' : 'text-site-muted'}`}>
        {waitEstimate.heading}
      </h2>
      <ul className="mt-1.5 space-y-1.5">
        {data.branches.map((wait) => (
          <WalkInWaitRow key={wait.branchId} wait={wait} now={now} onDark={onDark} />
        ))}
      </ul>
      {!onDark && <p className="mt-3 text-sm text-site-muted">{waitEstimate.note}</p>}
    </section>
  );
}
