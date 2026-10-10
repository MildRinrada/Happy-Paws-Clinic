import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { queueBoard } from '../content/queue-board';

interface Props {
  branchId: string;
  /** `queue:write`: may make and stop the waiting-room screens' tokens. */
  canManageDisplays: boolean;
}

/**
 * The board's way to its history, to the waiting-room screens, and to the staff-session screen,
 * which opens in its own tab pinned to this branch, so the TV keeps showing it whatever branch the
 * console switches to later.
 */
export function QueueNavLinks({ branchId, canManageDisplays }: Props) {
  return (
    <>
      <Link href="/queue/history" className={buttonClass('secondary')}>
        {queueBoard.links.history}
      </Link>
      {canManageDisplays && (
        <Link href="/queue/displays" title={queueBoard.links.displaysHint} className={buttonClass('secondary')}>
          {queueBoard.links.displays}
        </Link>
      )}
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
