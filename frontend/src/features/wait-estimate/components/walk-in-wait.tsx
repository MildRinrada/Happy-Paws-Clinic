import { loadWaitEstimates } from '../lib/load-wait-estimates';
import { WalkInWaitCard } from './walk-in-wait-card';

interface WalkInWaitProps {
  /** `hero` on the teal home hero, `plain` on a white page. */
  tone?: 'hero' | 'plain';
  /** Spacing from what sits above it, given by the page. */
  className?: string;
}

/**
 * "ตอนนี้รอประมาณ 20 ถึง 30 นาที (3 คิว)" for a walk-in, one line per branch. Read on the server
 * and shared by every visitor for 30 seconds (`load-wait-estimates.ts`). When the API is away
 * there is no block at all. Wrap it in `<Suspense fallback={null}>` so a slow read never holds up
 * the page.
 */
export async function WalkInWait({ tone = 'plain', className = '' }: WalkInWaitProps) {
  const data = await loadWaitEstimates();

  if (!data) return null;

  return (
    <div className={className}>
      <WalkInWaitCard data={data} tone={tone} now={new Date()} />
    </div>
  );
}
