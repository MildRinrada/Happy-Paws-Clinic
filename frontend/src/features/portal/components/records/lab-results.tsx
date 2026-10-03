import { formatLongDate } from '@/lib/format/datetime';
import { portalRecords } from '../../content/portal-records';
import type { OwnerLabResult } from '../../types/records.types';
import { EmptyLine } from '../shared/empty-line';
import { SectionCard } from '../shared/section-card';

/** Lab results the vet has already read. Values outside the range carry the flag the lab gave. */
export function LabResults({ results }: { results: OwnerLabResult[] }) {
  const text = portalRecords;

  return (
    <SectionCard title={text.sections.lab_result}>
      {results.length === 0 ? (
        <EmptyLine>{text.noneInVisit}</EmptyLine>
      ) : (
        <div className="space-y-5">
          {results.map((result) => (
            <div key={result.lab_request_id}>
              <p className="text-sm font-semibold text-ink-900">{result.tests.join(', ')}</p>
              {result.reviewed_at && (
                <p className="text-xs text-site-muted">{text.lab.reviewedOn(formatLongDate(result.reviewed_at))}</p>
              )}

              <table className="mt-2 w-full text-sm">
                <tbody className="divide-y divide-ink-100">
                  {result.values.map((value) => (
                    <tr key={value.analyte}>
                      <td className="py-1.5 pr-3 text-ink-700">{value.analyte}</td>
                      <td className="py-1.5 pr-3 text-right font-medium text-ink-900">
                        {value.value ?? value.value_text} {value.unit}
                      </td>
                      <td className="w-16 py-1.5 text-right">
                        {value.flag && value.flag !== 'normal' && (
                          <span className="rounded-full bg-amber-50 px-2 py-0.5 text-xs font-semibold text-amber-800">
                            {text.lab.flag[value.flag] ?? value.flag}
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ))}
        </div>
      )}
    </SectionCard>
  );
}
