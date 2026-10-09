import { formatLongDate } from '@/lib/format/datetime';
import { queueDisplay } from '../../content/queue-display';
import { DisplayClock } from './display-clock';
import { DisplayLive } from './display-live';

interface Props {
  branchId: string;
  branchName: string;
  serviceDate: string;
}

/** The top line of the screen: where, which day, the time, and whether it is still live. */
export function DisplayHeader({ branchId, branchName, serviceDate }: Props) {
  return (
    <header className="flex items-end justify-between gap-[2vw]">
      <div className="min-w-0">
        <p className="truncate font-display text-[clamp(1.5rem,4.5vh,4rem)] font-semibold leading-tight">
          {queueDisplay.heading(branchName)}
        </p>
        <p className="text-[clamp(1rem,2.6vh,2.25rem)] text-ink-300">{formatLongDate(serviceDate)}</p>
      </div>
      <div className="flex shrink-0 flex-col items-end gap-[0.5vh]">
        <p className="font-display text-[clamp(1.5rem,5vh,4.5rem)] font-semibold leading-none">
          <DisplayClock />
        </p>
        <DisplayLive branchId={branchId} />
      </div>
    </header>
  );
}
