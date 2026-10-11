import { Badge } from '@/components/ui/badge';
import { schedulePage } from '../content/schedule-page';

/**
 * "คาดคนไข้มาก" on a day the demand forecast (docs/contract-extensions.md § 33) expects to be
 * heavy, so whoever plans the roster sees it next to how many vets are on.
 */
export function ForecastMarker() {
  return (
    <span title={schedulePage.cover.busyHint}>
      <Badge tone="amber">{schedulePage.cover.busy}</Badge>
    </span>
  );
}
