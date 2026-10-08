import { businessReport } from '../content/business-report';
import { formatCount, formatMonth, formatPercent } from '../lib/format-report';
import type { RetentionAnalytics } from '../types/analytics.types';

/**
 * The cohort triangle: each row is a month's new customers, each column a month after. A cell shows
 * how many came back and what share of the cohort that is; the shade deepens with the share, and
 * the number is always printed, so the shade is never the only way to read it.
 */
export function CohortTable({ cohorts }: { cohorts: RetentionAnalytics['cohorts'] }) {
  const text = businessReport.retention;
  const width = Math.max(0, ...cohorts.map((cohort) => cohort.retained.length));

  return (
    <div className="space-y-1.5">
      <h4 className="text-xs font-semibold text-ink-700">{text.cohortTitle}</h4>
      <div className="overflow-x-auto rounded-lg border border-ink-100">
        <table className="w-full text-sm">
          <thead className="border-b border-ink-100 text-left text-ink-500">
            <tr>
              <th scope="col" className="px-3 py-2 font-medium">{text.cohortColumns.cohort}</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">{text.cohortColumns.customers}</th>
              {Array.from({ length: width }, (_, offset) => (
                <th key={offset} scope="col" className="px-3 py-2 text-right font-medium">
                  {text.monthOffset(offset)}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-ink-100 tabular-nums">
            {cohorts.map((cohort) => (
              <tr key={cohort.cohort}>
                <td className="px-3 py-2 text-ink-900">{formatMonth(cohort.cohort)}</td>
                <td className="px-3 py-2 text-right font-medium text-ink-900">{formatCount(cohort.customers)}</td>
                {Array.from({ length: width }, (_, offset) => {
                  const count = cohort.retained[offset];
                  const rate = cohort.retention_rates[offset];

                  if (count === undefined) {
                    return <td key={offset} className="px-3 py-2" />;
                  }

                  return (
                    <td
                      key={offset}
                      className="px-3 py-2 text-right text-ink-900"
                      style={
                        rate
                          ? { backgroundColor: `color-mix(in srgb, var(--color-brand-300) ${Math.round(15 + rate * 60)}%, transparent)` }
                          : undefined
                      }
                    >
                      {formatCount(count)}
                      <span className="ml-1 text-xs text-ink-600">{formatPercent(rate)}</span>
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
