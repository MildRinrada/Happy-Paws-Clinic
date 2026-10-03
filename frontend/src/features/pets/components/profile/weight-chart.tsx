import { formatDateTime } from '@/lib/format/datetime';
import { petProfile } from '../../content/pet-profile';
import type { WeightSeries } from '../../types/pet.types';

const WIDTH = 320;
const HEIGHT = 120;
const PAD = 14;

/**
 * The weight over time as a small line chart. Plain SVG: one series of a few dozen points does not
 * need a chart library. The points are spaced by date, so a long gap between visits looks long.
 */
export function WeightChart({ points }: { points: WeightSeries['points'] }) {
  const times = points.map((point) => Date.parse(point.recorded_at));
  const weights = points.map((point) => point.weight_kg);

  const [minT, maxT] = [Math.min(...times), Math.max(...times)];
  const [minW, maxW] = [Math.min(...weights), Math.max(...weights)];
  const spanT = maxT - minT || 1;
  const spanW = maxW - minW || 1;

  const x = (time: number) => PAD + ((time - minT) / spanT) * (WIDTH - PAD * 2);
  const y = (weight: number) => HEIGHT - PAD - ((weight - minW) / spanW) * (HEIGHT - PAD * 2);
  const coordinates = points.map((point, index) => [x(times[index]!), y(point.weight_kg)] as const);

  return (
    <figure>
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} className="h-32 w-full" role="img" aria-label={petProfile.weight.chartLabel}>
        <polyline
          points={coordinates.map(([cx, cy]) => `${cx},${cy}`).join(' ')}
          fill="none"
          className="stroke-brand-500"
          strokeWidth={2}
          strokeLinejoin="round"
        />
        {coordinates.map(([cx, cy], index) => (
          <circle key={points[index]!.consultation_id + index} cx={cx} cy={cy} r={3.5} className="fill-white stroke-brand-600" strokeWidth={2}>
            <title>
              {`${formatDateTime(points[index]!.recorded_at)} ${petProfile.weight.kg(points[index]!.weight_kg)}`}
            </title>
          </circle>
        ))}
      </svg>
      <figcaption className="flex justify-between text-xs text-ink-500">
        <span>{petProfile.weight.kg(minW)}</span>
        <span>{petProfile.weight.kg(maxW)}</span>
      </figcaption>
    </figure>
  );
}
