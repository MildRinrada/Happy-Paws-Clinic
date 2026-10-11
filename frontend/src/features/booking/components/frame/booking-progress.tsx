import { bookingFrame } from '../../content/booking-frame';
import { ProgressSegment } from './progress-segment';

interface BookingProgressProps {
  labels: string[];
  /** The index of the step on screen. */
  current: number;
  /** Go back to a finished step. Left out where the steps behind can no longer change. */
  onEdit?: (index: number) => void;
}

/**
 * The steps as bars: finished ones teal with "แก้ไข" under them, the current one orange, the rest
 * grey. On a phone the names give way to one line saying which step this is.
 */
export function BookingProgress({ labels, current, onEdit }: BookingProgressProps) {
  const text = bookingFrame.progress;

  return (
    <nav aria-label={text.label}>
      <ol className="grid gap-2 sm:gap-2.5" style={{ gridTemplateColumns: `repeat(${labels.length}, minmax(0, 1fr))` }}>
        {labels.map((label, index) => (
          <ProgressSegment
            key={label}
            label={`${index + 1}. ${label}`}
            state={index < current ? 'done' : index === current ? 'current' : 'next'}
            onEdit={index < current && onEdit ? () => onEdit(index) : undefined}
          />
        ))}
      </ol>
      <p className="mt-3 text-sm text-site-muted sm:hidden">
        {text.stepOf(current + 1, labels.length)}{' '}
        <span className="font-bold text-site-forest">{labels[current]}</span>
      </p>
    </nav>
  );
}
