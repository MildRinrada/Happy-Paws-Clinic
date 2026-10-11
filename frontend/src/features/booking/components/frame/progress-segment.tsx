import { bookingFrame } from '../../content/booking-frame';

type SegmentState = 'done' | 'current' | 'next';

const bars: Record<SegmentState, string> = {
  done: 'bg-site-teal',
  current: 'bg-site-orange',
  next: 'bg-ink-200',
};

const words: Record<SegmentState, string> = {
  done: 'text-ink-800',
  current: 'font-bold text-site-forest',
  next: 'text-site-muted',
};

/** One step of the progress bar: its bar, its name, and "แก้ไข" under a finished one. */
export function ProgressSegment({ label, state, onEdit }: { label: string; state: SegmentState; onEdit?: () => void }) {
  return (
    <li aria-current={state === 'current' ? 'step' : undefined} className="min-w-0">
      <span aria-hidden="true" className={`block h-[5px] rounded-full ${bars[state]}`} />
      <span className={`mt-2.5 hidden text-[15px] sm:block ${words[state]}`}>{label}</span>
      <span className="sr-only sm:hidden">{label}</span>
      {onEdit && (
        <button
          type="button"
          onClick={onEdit}
          aria-label={bookingFrame.progress.editLabel(label)}
          className="mt-1.5 text-[13px] font-semibold text-site-forest underline underline-offset-2 hover:text-site-teal"
        >
          {bookingFrame.progress.edit}
        </button>
      )}
    </li>
  );
}
