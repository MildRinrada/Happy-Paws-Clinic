import { waitLine } from '../lib/wait-line';
import type { BranchWait } from '../types/wait-estimate.types';

interface WalkInWaitRowProps {
  wait: BranchWait;
  now: Date;
  /** Text colours for the teal hero or a white page. */
  onDark: boolean;
}

/** One branch: a dot for open or closed, the branch name when there are several, and the wait. */
export function WalkInWaitRow({ wait, now, onDark }: WalkInWaitRowProps) {
  const { text, detail } = waitLine(wait.estimate, now);
  const open = wait.estimate.open_now;

  return (
    <li className="flex items-start gap-2.5">
      <span
        aria-hidden="true"
        className={`mt-[7px] size-2.5 shrink-0 rounded-full ${open ? 'bg-stat-green' : onDark ? 'bg-white/60' : 'bg-site-alert'}`}
      />
      <p className="min-w-0 text-[15px] leading-snug">
        {wait.branchName && <span className="block font-bold">{wait.branchName}</span>}
        <span className="font-semibold">{text}</span>
        {detail && <span className={onDark ? 'text-white/80' : 'text-site-muted'}> {detail}</span>}
      </p>
    </li>
  );
}
