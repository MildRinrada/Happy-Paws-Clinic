import { labPage } from '../../content/lab-page';
import { valueText } from '../../lib/previous-values';
import type { LabResult } from '../../types/lab.types';

/**
 * "แก้ไขแล้ว" beside a corrected value, opening to the value it replaced and why (§ 52). A native
 * disclosure, so it opens by keyboard and by tap as well as showing its text on hover.
 */
export function LabAmendedNote({ result }: { result: LabResult }) {
  const before = result.previous_values.at(-1);
  if (!result.amended || !before) return null;

  const text = labPage.results;
  const detail = text.amendedDetail(`${valueText(before)}${before.unit ? ` ${before.unit}` : ''}`, before.reason);

  return (
    <details className="group mt-0.5 text-xs font-normal">
      <summary title={detail} className="inline-flex cursor-pointer list-none items-center gap-1 rounded text-amber-800 hover:underline">
        <span aria-hidden>✎</span>
        {text.amended}
      </summary>
      <p className="mt-1 max-w-64 whitespace-normal text-left text-ink-600">{detail}</p>
    </details>
  );
}
