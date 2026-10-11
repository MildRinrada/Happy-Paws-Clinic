import { petDetail } from '../../../content/pet-detail';
import type { WeightPoint } from '../../../lib/load-pet-weights';
import { thaiDate, thaiDayMonth, thaiMonthYear } from '../../../lib/pet-dates';
import { barShares } from './weight-scale';

interface WeightBarsProps {
  points: WeightPoint[];
  /** `full` writes each value over its bar and the month under it; `spark` is the bars alone. */
  variant: 'full' | 'spark';
}

/** Months under the bars, or day and month when two weighings share a month. */
function axisLabels(points: WeightPoint[]): string[] {
  const months = points.map((point) => thaiMonthYear(point.date));
  return new Set(months).size === months.length ? months : points.map((point) => thaiDayMonth(point.date));
}

/** Tallest bar in pixels, for each look. */
const BAR_HEIGHT = { full: 104, spark: 72 };

/**
 * A pet's recent weights as bars, oldest on the left and the latest in dark green. Plain divs, no
 * chart library. The drawing is hidden from screen readers, which hear the same values as a list.
 */
export function WeightBars({ points, variant }: WeightBarsProps) {
  const text = petDetail.weight;
  const shares = barShares(points);
  const labels = axisLabels(points);
  const full = variant === 'full';
  const last = points.length - 1;

  return (
    <figure>
      <div aria-hidden="true">
        <div className={`flex items-end ${full ? 'gap-2.5 border-b border-ink-200' : 'gap-2.5'}`}>
          {points.map((point, index) => (
            <div key={point.date} className="flex min-w-0 flex-1 flex-col items-center" title={text.point(thaiDate(point.date), point.kg)}>
              {full && (
                <span className={`mb-1.5 font-display text-xs leading-4 ${index === last ? 'font-semibold text-brand-900' : 'text-ink-600'}`}>
                  {point.kg.toFixed(1)}
                </span>
              )}
              <span
                className={`block w-full rounded-t-md ${index === last ? 'bg-brand-600' : 'bg-brand-200'}`}
                style={{ height: Math.round((shares[index] ?? 0) * BAR_HEIGHT[variant]) }}
              />
            </div>
          ))}
        </div>

        {full && (
          <div className="mt-2.5 flex gap-2.5">
            {labels.map((label, index) => (
              <span key={points[index]?.date ?? index} className="min-w-0 flex-1 truncate text-center text-xs leading-5 text-ink-600">
                {label}
              </span>
            ))}
          </div>
        )}
      </div>

      <figcaption className="sr-only">
        {text.summary(points.length)}
        <ul>
          {points.map((point) => (
            <li key={point.date}>{text.point(thaiDate(point.date), point.kg)}</li>
          ))}
        </ul>
      </figcaption>
    </figure>
  );
}
