import { labPage } from '../content/lab-page';
import type { LabResult } from '../types/lab.types';
import { LabFlagBadge } from './lab-flag-badge';

function range(result: LabResult): string {
  if (result.reference_low !== undefined || result.reference_high !== undefined) {
    return `${result.reference_low ?? ''} - ${result.reference_high ?? ''}`.trim();
  }
  return result.reference_text ?? '';
}

/** The measured values in the order they arrived, an abnormal one in red. */
export function LabResultTable({ results }: { results: LabResult[] }) {
  const text = labPage.results;

  if (results.length === 0) return <p className="text-sm text-ink-500">{text.none}</p>;

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[420px] text-sm">
        <thead className="text-left text-xs text-ink-500">
          <tr>
            <th scope="col" className="py-1.5 pr-3 font-medium">{text.analyte}</th>
            <th scope="col" className="py-1.5 pr-3 text-right font-medium">{text.value}</th>
            <th scope="col" className="py-1.5 pr-3 font-medium">{text.range}</th>
            <th scope="col" className="py-1.5 font-medium">{text.flag}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100">
          {results.map((result) => {
            const abnormal = result.flag !== undefined && result.flag !== 'normal';

            return (
              <tr key={result.id}>
                <td className="py-1.5 pr-3 text-ink-800">{result.analyte}</td>
                <td className={`py-1.5 pr-3 text-right tabular-nums ${abnormal ? 'font-semibold text-red-700' : 'text-ink-900'}`}>
                  {result.value_numeric ?? result.value_text}
                  {result.unit && <span className="ml-1 text-xs font-normal text-ink-500">{result.unit}</span>}
                </td>
                <td className="py-1.5 pr-3 tabular-nums text-ink-600">{range(result)}</td>
                <td className="py-1.5">
                  <LabFlagBadge flag={result.flag} />
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
