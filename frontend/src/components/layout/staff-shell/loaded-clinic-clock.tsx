import { getOpeningHours } from '@/features/clinic/api/getOpeningHours';
import { ClinicClock } from './clinic-clock';

interface LoadedClinicClockProps {
  now: string;
  branchId?: string;
}

/**
 * The clock once the clinic's hours are in (`GET /clinic/hours`, public). Hours the clinic has not
 * set, or a read that failed, leave the open or closed status off rather than guessing it.
 */
export async function LoadedClinicClock({ now, branchId }: LoadedClinicClockProps) {
  const hours = await getOpeningHours().catch(() => null);

  return <ClinicClock now={now} branchId={branchId} hours={hours?.configured ? hours : null} />;
}
