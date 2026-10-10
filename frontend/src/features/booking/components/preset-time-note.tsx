import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { bookingPage } from '../content/booking-page';

/** "เวลาที่เลือกไว้ ...": the time the owner arrived with, kept while they choose a pet. */
export function PresetTimeNote({ startsAt }: { startsAt: string }) {
  return (
    <p role="status" className="rounded-xl bg-site-soft px-4 py-3 text-sm font-semibold text-ink-800">
      {bookingPage.preset.note(`${formatLongDate(startsAt)} ${formatTime(startsAt)}`)}
    </p>
  );
}
