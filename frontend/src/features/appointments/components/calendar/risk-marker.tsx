import { attendanceRiskText } from '../../content/attendance-risk';
import type { AttendanceRiskRow } from '../../types/attendance-risk.types';

/**
 * A small mark on a grid block, where a whole badge does not fit. The reasons are its tooltip and
 * its accessible name; the panel that opens on a press shows them in full.
 */
export function RiskMarker({ risk }: { risk?: AttendanceRiskRow }) {
  if (!risk || risk.risk === 'low') {
    return null;
  }

  const reasons = risk.reasons.join(', ');

  return (
    <span
      className={`rounded px-1 text-[10px] font-semibold text-white ${risk.risk === 'high' ? 'bg-red-600' : 'bg-amber-500'}`}
      title={reasons}
      aria-label={reasons}
    >
      {attendanceRiskText.marker}
    </span>
  );
}
