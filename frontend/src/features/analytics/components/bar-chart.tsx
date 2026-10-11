import { reportsPage } from '../content/reports-page';

export interface BarPoint {
  key: string;
  /** Under the axis and in the hover text. */
  label: string;
  value: number;
  /** The value as the hover text shows it, e.g. formatted as baht. */
  display: string;
}

const WIDTH = 640;
const HEIGHT = 120;
const MAX_BAR = 28;
const GAP = 2;
/** Room above the tallest bar, where the dashed line of the highest value runs. */
const PLOT_TOP = 5;

/** A bar with a 4px rounded data end and a square foot on the baseline. */
function barPath(x: number, width: number, top: number, bottom: number): string {
  const radius = Math.min(4, width / 2, bottom - top);

  return [
    `M${x},${bottom}`,
    `V${top + radius}`,
    `Q${x},${top} ${x + radius},${top}`,
    `H${x + width - radius}`,
    `Q${x + width},${top} ${x + width},${top + radius}`,
    `V${bottom}`,
    'Z',
  ].join(' ');
}

/**
 * One series as vertical bars, in plain SVG like the weight chart on the pet page: a single series
 * of a few dozen points does not need a chart library. Every bar has a hover title over a hit area
 * the full height of its slot, so a short bar is as easy to point at as a tall one. The axis labels
 * are the first, middle and last periods; the table beside each chart has every value.
 *
 * The y axis has one label, the highest value, set above the chart beside a faint line at the
 * height of the tallest bar: enough to read the scale without a grid.
 *
 * A negative value (a day with more refunded than taken) is drawn as no bar; its hover text still
 * gives the number.
 */
export function BarChart({ points, label }: { points: BarPoint[]; label: string }) {
  if (points.length === 0) {
    return null;
  }

  const max = Math.max(1, ...points.map((point) => point.value));
  const slot = WIDTH / points.length;
  const barWidth = Math.max(1, Math.min(MAX_BAR, slot - GAP));
  const ticks = [...new Set([0, Math.floor((points.length - 1) / 2), points.length - 1])].map((index) => points[index]!);
  const peak = points.reduce((best, point) => (point.value > best.value ? point : best), points[0]!);

  return (
    <figure className="space-y-1">
      {peak.value > 0 && <p className="text-xs tabular-nums text-ink-500">{reportsPage.chartMax(peak.display)}</p>}
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} className="h-28 w-full" role="img" aria-label={label} preserveAspectRatio="none">
        <line x1={0} x2={WIDTH} y1={HEIGHT - 0.5} y2={HEIGHT - 0.5} className="stroke-ink-200" strokeWidth={1} />
        {peak.value > 0 && (
          <line
            x1={0}
            x2={WIDTH}
            y1={PLOT_TOP - 0.5}
            y2={PLOT_TOP - 0.5}
            className="stroke-ink-200"
            strokeWidth={1}
            strokeDasharray="4 4"
            vectorEffect="non-scaling-stroke"
          />
        )}
        {points.map((point, index) => {
          const x = index * slot + (slot - barWidth) / 2;
          const height = point.value > 0 ? Math.max(2, (point.value / max) * (HEIGHT - 1 - PLOT_TOP)) : 0;

          return (
            <g key={point.key}>
              <title>{`${point.label}: ${point.display}`}</title>
              <rect x={index * slot} y={0} width={slot} height={HEIGHT} className="fill-transparent hover:fill-ink-100/60" />
              {height > 0 && (
                <path d={barPath(x, barWidth, HEIGHT - 1 - height, HEIGHT - 1)} className="pointer-events-none fill-brand-500" />
              )}
            </g>
          );
        })}
      </svg>
      <figcaption className="flex justify-between text-xs text-ink-500">
        {ticks.map((point) => (
          <span key={point.key}>{point.label}</span>
        ))}
      </figcaption>
    </figure>
  );
}
