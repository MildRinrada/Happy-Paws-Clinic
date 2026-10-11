import { labPage } from '../../content/lab-page';
import { valueText } from '../../lib/previous-values';
import type { LabResult } from '../../types/lab.types';
import { LabAmendedNote } from './lab-amended-note';

/**
 * A measured value: red and bold when out of range, crossed through with its reason when voided,
 * and marked when it was corrected. A voided value is shown, not hidden: someone may have read it.
 */
export function LabValueCell({ result }: { result: LabResult }) {
  const text = labPage.results;
  const abnormal = !result.voided && result.flag !== undefined && result.flag !== 'normal';

  if (result.voided) {
    return (
      <td className="py-1.5 pr-3 text-right align-top">
        <s className="tabular-nums text-ink-400">
          {valueText(result)}
          {result.unit && <span className="ml-1 text-xs">{result.unit}</span>}
        </s>
        <span className="block text-xs text-ink-500">
          {text.voided}
          {result.void_reason && <span className="sr-only">{text.voidedDetail(result.void_reason)}</span>}
        </span>
        {result.void_reason && (
          <span aria-hidden className="block max-w-56 text-xs text-ink-400">
            {text.voidedDetail(result.void_reason)}
          </span>
        )}
      </td>
    );
  }

  return (
    <td className={`py-1.5 pr-3 text-right align-top tabular-nums ${abnormal ? 'font-semibold text-red-700' : 'text-ink-900'}`}>
      {valueText(result)}
      {result.unit && <span className="ml-1 text-xs font-normal text-ink-500">{result.unit}</span>}
      <LabAmendedNote result={result} />
    </td>
  );
}
