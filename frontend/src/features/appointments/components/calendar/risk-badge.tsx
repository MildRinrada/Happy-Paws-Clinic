import { Badge } from '@/components/ui/badge';
import { riskLabels } from '../../content/attendance-risk';
import type { AttendanceRiskRow } from '../../types/attendance-risk.types';

/**
 * "เสี่ยงไม่มา" on a medium or high appointment, with the reasons on hover. Nothing for a low one:
 * a board full of "ปกติ" says nothing.
 */
export function RiskBadge({ risk }: { risk?: AttendanceRiskRow }) {
  if (!risk || risk.risk === 'low') {
    return null;
  }

  const { label, tone } = riskLabels[risk.risk];

  return (
    <span title={risk.reasons.join('\n')}>
      <Badge tone={tone}>{label}</Badge>
    </span>
  );
}
