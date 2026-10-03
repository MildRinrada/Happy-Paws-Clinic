import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatDateTime } from '@/lib/format/datetime';
import { petProfile } from '../../content/pet-profile';
import type { PetDetail, WeightSeries } from '../../types/pet.types';
import { WeightChart } from './weight-chart';

/** The latest weight, and the graph when there are two readings or more. */
export function WeightCard({ latest, series }: { latest: PetDetail['latest_weight']; series: WeightSeries | null }) {
  const text = petProfile.weight;
  const points = series?.points ?? [];

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody className="space-y-3">
        {latest ? (
          <p>
            <span className="font-display text-2xl font-semibold text-ink-900">{text.kg(latest.weight_kg)}</span>{' '}
            <span className="text-sm text-ink-500">
              {text.latest} {formatDateTime(latest.recorded_at)}
            </span>
          </p>
        ) : (
          <p className="text-sm text-ink-500">{text.none}</p>
        )}
        {points.length > 1 && <WeightChart points={points} />}
      </CardBody>
    </Card>
  );
}
