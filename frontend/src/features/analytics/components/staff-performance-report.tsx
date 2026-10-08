import { EmptyState } from '@/components/ui/empty-state';
import { formatBaht } from '@/lib/format/money';
import { businessReport } from '../content/business-report';
import { formatCount, formatMinutes } from '../lib/format-report';
import type { StaffPerformance } from '../types/analytics.types';
import { ReportCard } from './report-card';

/** Cases and money per person. Shown only to a session holding `analytics:hr`. */
export function StaffPerformanceReport({ performance }: { performance: StaffPerformance }) {
  const text = businessReport.staff;

  return (
    <ReportCard title={text.title} note={text.note}>
      {performance.staff.length === 0 ? (
        <EmptyState title={text.empty} />
      ) : (
        <div className="overflow-x-auto rounded-lg border border-ink-100">
          <table className="w-full min-w-[640px] text-sm">
            <thead className="border-b border-ink-100 text-left text-ink-500">
              <tr>
                <th scope="col" className="px-3 py-2 font-medium">{text.columns.name}</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.cases}</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.averageCase}</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.treatments}</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.attributed}</th>
                <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.collected}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink-100 tabular-nums">
              {performance.staff.map((person) => (
                <tr key={person.staff_id}>
                  <td className="px-3 py-2 text-ink-900">{person.display_name}</td>
                  <td className="px-3 py-2 text-right">{formatCount(person.cases)}</td>
                  <td className="px-3 py-2 text-right">{formatMinutes(person.average_case_minutes)}</td>
                  <td className="px-3 py-2 text-right">{formatCount(person.treatments)}</td>
                  <td className="px-3 py-2 text-right font-medium text-ink-900">
                    {formatBaht(person.attributed_revenue_satang)}
                  </td>
                  <td className="px-3 py-2 text-right">{formatBaht(person.collected_satang)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </ReportCard>
  );
}
