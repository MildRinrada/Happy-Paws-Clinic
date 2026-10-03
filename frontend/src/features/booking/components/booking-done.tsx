import Link from 'next/link';
import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { bookingPage } from '../content/booking-page';

/** The booking went through: when it is, and where to go next. */
export function BookingDone({ startsAt }: { startsAt: string }) {
  const text = bookingPage.done;

  return (
    <div className="space-y-4 text-center">
      <h2 className="text-lg font-semibold text-ink-900">{text.title}</h2>
      <p className="font-display text-2xl font-bold text-site-teal">
        {formatLongDate(startsAt)} {formatTime(startsAt)}
      </p>
      <p className="text-sm text-site-muted">{text.body}</p>

      <div className="flex flex-col gap-3 sm:flex-row sm:justify-center">
        <Link href="/portal/appointments" className="inline-flex h-11 items-center justify-center rounded-xl bg-site-teal px-5 text-sm font-semibold text-white">
          {text.appointments}
        </Link>
        <Link href="/portal" className="inline-flex h-11 items-center justify-center rounded-xl border border-ink-200 px-5 text-sm font-semibold text-ink-700">
          {text.home}
        </Link>
      </div>
    </div>
  );
}
