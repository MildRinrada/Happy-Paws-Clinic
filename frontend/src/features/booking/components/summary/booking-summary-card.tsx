import { useId, type ReactNode } from 'react';
import { bookingFrame } from '../../content/booking-frame';

/**
 * "สรุปการจอง": the dark green card beside the step, with what has been chosen so far. `action` is
 * the step's next button again, shown on a wide screen only: on a phone the card sits under the
 * step, whose own button is right above it.
 */
export function BookingSummaryCard({ children, action }: { children: ReactNode; action?: ReactNode }) {
  const headingId = useId();

  return (
    <section aria-labelledby={headingId} className="rounded-[18px] bg-site-forest p-6 text-white">
      <h2 id={headingId} className="text-lg font-bold">
        {bookingFrame.summary.title}
      </h2>
      <dl className="mt-5 space-y-[18px]">{children}</dl>
      {action && <div className="mt-6 hidden lg:block">{action}</div>}
    </section>
  );
}
