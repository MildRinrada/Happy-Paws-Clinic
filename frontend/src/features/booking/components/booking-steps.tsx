import { bookingPage } from '../content/booking-page';

/**
 * "1 รายละเอียด, 2 วันและเวลา, 3 ยืนยัน" with the current step filled in. The public booking page
 * passes its own five labels.
 */
export function BookingSteps({ current, labels = bookingPage.steps }: { current: number; labels?: string[] }) {
  return (
    <ol className="flex flex-wrap items-center gap-2 text-sm">
      {labels.map((label, index) => {
        const state = index < current ? 'done' : index === current ? 'current' : 'next';

        return (
          <li key={label} aria-current={state === 'current' ? 'step' : undefined} className="flex items-center gap-2">
            <span
              className={`flex size-7 items-center justify-center rounded-full text-xs font-bold ${
                state === 'next' ? 'bg-ink-100 text-ink-500' : 'bg-site-teal text-white'
              }`}
            >
              {index + 1}
            </span>
            <span className={state === 'current' ? 'font-semibold text-ink-900' : 'hidden text-ink-500 sm:inline'}>
              {label}
            </span>
            {index < labels.length - 1 && <span aria-hidden="true" className="mx-1 h-px w-6 bg-ink-200" />}
          </li>
        );
      })}
    </ol>
  );
}
