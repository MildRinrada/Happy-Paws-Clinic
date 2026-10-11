import { labPage } from '../content/lab-page';
import { analyteKey, type PreviousValues } from '../lib/previous-values';
import type { LabResult } from '../types/lab.types';
import { CorrectResultButton } from './corrections/correct-result-button';
import { LabFlagBadge } from './lab-flag-badge';
import { LabPreviousCell } from './results/lab-previous-cell';
import { LabValueCell } from './results/lab-value-cell';

function range(result: LabResult): string {
  if (result.reference_low !== undefined || result.reference_high !== undefined) {
    return `${result.reference_low ?? ''} - ${result.reference_high ?? ''}`.trim();
  }
  return result.reference_text ?? '';
}

interface LabResultTableProps {
  results: LabResult[];
  /**
   * The pet's previous values for the "ครั้งก่อน" column. `undefined` leaves the column out; an
   * empty record keeps it, empty, while the history loads or when there is none.
   */
  previous?: PreviousValues;
  /** Given with `lab:write`: each value gets "แก้ไข" (§ 52), sent against this version. */
  correction?: { labRequestId: string; version: number };
}

/**
 * The measured values in the order they arrived, an abnormal one in red, a corrected one marked
 * and a voided one crossed through. Draws no dates, so it renders the same in the browser as on
 * the server and can sit inside the worklist's client row.
 */
export function LabResultTable({ results, previous, correction }: LabResultTableProps) {
  const text = labPage.results;

  if (results.length === 0) return <p className="text-sm text-ink-500">{text.none}</p>;

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[460px] text-sm">
        <thead className="text-left text-xs text-ink-500">
          <tr>
            <th scope="col" className="py-1.5 pr-3 font-medium">{text.analyte}</th>
            <th scope="col" className="py-1.5 pr-3 text-right font-medium">{text.value}</th>
            {previous && <th scope="col" className="py-1.5 pr-3 text-right font-medium">{text.previous}</th>}
            <th scope="col" className="py-1.5 pr-3 font-medium">{text.range}</th>
            <th scope="col" className="py-1.5 pr-3 font-medium">{text.flag}</th>
            {correction && (
              <th scope="col" className="py-1.5 font-medium">
                <span className="sr-only">{text.actions}</span>
              </th>
            )}
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {results.map((result) => (
            <tr key={result.id} className={result.voided ? 'bg-ink-50/60' : undefined}>
              <td className={`py-1.5 pr-3 align-top ${result.voided ? 'text-ink-400 line-through' : 'text-ink-800'}`}>{result.analyte}</td>
              <LabValueCell result={result} />
              {previous && <LabPreviousCell result={result} previous={previous[analyteKey(result.analyte)]} />}
              <td className="py-1.5 pr-3 align-top tabular-nums text-ink-600">{range(result)}</td>
              <td className="py-1.5 pr-3 align-top">{!result.voided && <LabFlagBadge flag={result.flag} />}</td>
              {correction && (
                <td className="py-0.5 text-right align-top">
                  {!result.voided && <CorrectResultButton labRequestId={correction.labRequestId} version={correction.version} result={result} />}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
