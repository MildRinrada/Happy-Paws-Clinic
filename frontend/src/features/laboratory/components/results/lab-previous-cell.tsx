import { labPage } from '../../content/lab-page';
import { trendOf, type PreviousValue } from '../../lib/previous-values';
import type { LabResult } from '../../types/lab.types';

const arrows = {
  up: { path: 'M10 15V5M5.5 9.5 10 5l4.5 4.5', label: labPage.results.rose },
  down: { path: 'M10 5v10M5.5 10.5 10 15l4.5-4.5', label: labPage.results.fell },
  same: { path: 'M4.5 10h11', label: labPage.results.same },
} as const;

/**
 * "ครั้งก่อน": the same value from the pet's previous request, with an arrow for which way it moved.
 * The arrow is a picture; the words for it are there for a screen reader. Empty when there is no
 * earlier value, or while it is loading.
 */
export function LabPreviousCell({ result, previous }: { result: LabResult; previous?: PreviousValue }) {
  if (!previous || result.voided) return <td className="py-1.5 pr-3 align-top" />;

  const trend = trendOf(result, previous);
  const shown = `${previous.text}${previous.unit && previous.unit !== result.unit ? ` ${previous.unit}` : ''}`;

  return (
    <td className="py-1.5 pr-3 text-right align-top tabular-nums text-ink-500">
      <span className="inline-flex items-center justify-end gap-1">
        <span aria-hidden>{shown}</span>
        {trend && (
          <svg aria-hidden viewBox="0 0 20 20" className="size-4 text-ink-500" fill="none" stroke="currentColor" strokeWidth="2">
            <path d={arrows[trend].path} strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        )}
        {trend ? <span className="sr-only">{arrows[trend].label(shown)}</span> : <span className="sr-only">{labPage.results.was(shown)}</span>}
      </span>
    </td>
  );
}
