import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { VisitLine } from './visit-line';

interface AppointmentSummaryProps {
  service: string;
  vet: string;
  startsAt: string;
}

/** An appointment on a later day: what, with whom, the date, and the time large. */
export function AppointmentSummary({ service, vet, startsAt }: AppointmentSummaryProps) {
  return (
    <div className="min-w-0">
      <VisitLine text={service} vet={vet} />
      <p className="mt-3 text-xs leading-5 text-brand-100">{formatLongDate(startsAt)}</p>
      <p className="font-display text-[40px] font-bold leading-[48px] text-white">{formatTime(startsAt)}</p>
    </div>
  );
}
