import { attendanceRiskText } from '../../content/attendance-risk';
import type { AttendanceRiskRow } from '../../types/attendance-risk.types';
import { RiskBadge } from './risk-badge';

/** The risk and every reason behind it, in the appointment's panel. */
export function RiskReasons({ risk }: { risk?: AttendanceRiskRow }) {
  if (!risk || risk.risk === 'low' || risk.reasons.length === 0) {
    return null;
  }

  return (
    <div className="space-y-1 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-900">
      <RiskBadge risk={risk} />
      <p className="sr-only">{attendanceRiskText.reasonsLabel}</p>
      <ul className="list-disc space-y-0.5 pl-5">
        {risk.reasons.map((reason) => (
          <li key={reason}>{reason}</li>
        ))}
      </ul>
    </div>
  );
}
