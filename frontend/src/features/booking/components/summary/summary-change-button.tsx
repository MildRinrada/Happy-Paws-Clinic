import { bookingFrame } from '../../content/booking-frame';

/** "เปลี่ยน": the yellow link beside the service on the summary card, back to the details step. */
export function SummaryChangeButton({ onClick }: { onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={bookingFrame.summary.changeLabel}
      className="shrink-0 text-sm font-semibold text-site-sun underline underline-offset-2 hover:brightness-110"
    >
      {bookingFrame.summary.change}
    </button>
  );
}
