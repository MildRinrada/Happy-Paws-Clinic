import { Suspense } from 'react';
import { ClinicClock } from './clinic-clock';
import { LoadedClinicClock } from './loaded-clinic-clock';

/** The top bar's clock, drawn at once; the open or closed status streams in with the hours. */
export function TopBarClock({ now, branchId }: { now: string; branchId?: string }) {
  return (
    <Suspense fallback={<ClinicClock now={now} />}>
      <LoadedClinicClock now={now} branchId={branchId} />
    </Suspense>
  );
}
