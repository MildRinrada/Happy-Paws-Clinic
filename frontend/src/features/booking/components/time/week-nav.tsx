import { bookingFrame } from '../../content/booking-frame';

interface WeekNavProps {
  canGoBack: boolean;
  canGoOn: boolean;
  onBack: () => void;
  onOn: () => void;
}

/** "สัปดาห์ก่อน" and "สัปดาห์ถัดไป" beside the day label: the next seven chips, or the seven before. */
export function WeekNav({ canGoBack, canGoOn, onBack, onOn }: WeekNavProps) {
  const text = bookingFrame.time;
  const look =
    'text-[13px] font-semibold text-site-forest underline underline-offset-2 hover:text-site-teal disabled:cursor-not-allowed disabled:text-ink-400 disabled:no-underline';

  return (
    <div className="flex items-center gap-4">
      <button type="button" onClick={onBack} disabled={!canGoBack} className={look}>
        <span aria-hidden="true">‹ </span>
        {text.previousWeek}
      </button>
      <button type="button" onClick={onOn} disabled={!canGoOn} className={look}>
        {text.nextWeek}
        <span aria-hidden="true"> ›</span>
      </button>
    </div>
  );
}
