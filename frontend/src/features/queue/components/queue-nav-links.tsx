import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { queueBoard } from '../content/queue-board';

/**
 * The board's way to its history and to the waiting-room screen, which opens in its own tab pinned
 * to this branch, so the TV keeps showing it whatever branch the console switches to later.
 */
export function QueueNavLinks({ branchId }: { branchId: string }) {
  return (
    <>
      <Link href="/queue/history" className={buttonClass('secondary')}>
        {queueBoard.links.history}
      </Link>
      <a
        href={`/queue/display?branch=${encodeURIComponent(branchId)}`}
        target="_blank"
        rel="noopener"
        title={queueBoard.links.displayHint}
        className={buttonClass('secondary')}
      >
        {queueBoard.links.display}
      </a>
    </>
  );
}
