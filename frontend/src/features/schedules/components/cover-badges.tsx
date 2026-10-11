import { Badge } from '@/components/ui/badge';
import { schedulePage } from '../content/schedule-page';
import type { DayCover } from '../lib/roster-summary';
import { ForecastMarker } from './forecast-marker';

interface CoverBadgesProps {
  cover: DayCover;
  /** True when the demand forecast expects the day to be heavy. */
  busy: boolean;
  /** Also say how many are on the front desk. */
  withFrontDesk?: boolean;
}

/** How many vets are on a day, red when none, and the forecast's warning beside it. */
export function CoverBadges({ cover, busy, withFrontDesk = false }: CoverBadgesProps) {
  const text = schedulePage.cover;

  return (
    <span className="flex flex-wrap gap-1">
      <Badge tone={cover.vets === 0 ? 'red' : 'neutral'}>{cover.vets === 0 ? text.noVet : text.vets(cover.vets)}</Badge>
      {withFrontDesk && <Badge tone={cover.frontDesk === 0 ? 'amber' : 'neutral'}>{text.frontDesk(cover.frontDesk)}</Badge>}
      {busy && <ForecastMarker />}
    </span>
  );
}
