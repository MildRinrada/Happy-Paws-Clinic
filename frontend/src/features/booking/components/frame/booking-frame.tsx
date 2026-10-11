import type { ReactNode } from 'react';

interface BookingFrameProps {
  /** `guest` on `/book`, `portal` on `/portal/book`; carried as `data-booking-flow`. */
  flow: 'guest' | 'portal';
  /** The step's own name, carried as `data-booking-step` so how far people get can be measured later. */
  step: string;
  progress: ReactNode;
  /** A line across the full width under the progress bar, such as "มีคนชวนมา". */
  note?: ReactNode;
  /** The summary card and the notes under it. Beside the step on a wide screen, under it on a phone. */
  aside: ReactNode;
  children: ReactNode;
}

/** The booking pages' layout: the progress bar, then the step's card with the summary at its right. */
export function BookingFrame({ flow, step, progress, note, aside, children }: BookingFrameProps) {
  return (
    <div className="space-y-6" data-booking-flow={flow} data-booking-step={step}>
      {progress}
      {note}
      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_286px] lg:items-start lg:gap-6">
        <div className="min-w-0">{children}</div>
        <div className="space-y-4">{aside}</div>
      </div>
    </div>
  );
}
