import { Badge } from '@/components/ui/badge';
import { confidenceLabels } from '../../content/forecast-page';
import type { ForecastConfidence } from '../../types/forecast.types';

/** How much history the forecast stands on. Only the doubtful ones need saying. */
export function ForecastConfidenceBadge({ confidence }: { confidence: ForecastConfidence }) {
  if (confidence === 'high') {
    return null;
  }

  const { label, tone } = confidenceLabels[confidence];

  return <Badge tone={tone}>{label}</Badge>;
}
