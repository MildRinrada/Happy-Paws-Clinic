import { formatLongDate, formatTime } from '@/lib/format/datetime';

interface AppointmentSummaryProps {
  service: string;
  vet: string;
  startsAt: string;
  /** Adds the date above the time, for a visit that is not today. */
  showDate: boolean;
}

/** What the visit is, with whom, and the time, large. */
export function AppointmentSummary({ service, vet, startsAt, showDate }: AppointmentSummaryProps) {
  return (
    <div className="min-w-0">
      <p className="text-sm font-semibold leading-snug text-white">{service}</p>
      <p className="text-sm leading-snug text-white/90">{vet}</p>
      {showDate && <p className="mt-1 text-xs text-white/80">{formatLongDate(startsAt)}</p>}
      <p className="mt-1 font-display text-[40px] font-bold leading-none text-white">{formatTime(startsAt)}</p>
    </div>
  );
}
