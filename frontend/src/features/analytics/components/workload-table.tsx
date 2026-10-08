import { EmptyState } from '@/components/ui/empty-state';
import { operationsReport } from '../content/operations-report';
import { formatCount, formatMinutes } from '../lib/format-report';
import type { WorkloadStats } from '../types/analytics.types';

/** One row per vet, busiest first, as the API orders them. */
export function WorkloadTable({ staff }: { staff: WorkloadStats['staff'] }) {
  const text = operationsReport.workload;

  if (staff.length === 0) {
    return <EmptyState title={text.empty} />;
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-ink-100">
      <table className="w-full min-w-[560px] text-sm">
        <thead className="border-b border-ink-100 text-left text-ink-500">
          <tr>
            <th scope="col" className="px-3 py-2 font-medium">{text.columns.name}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.cases}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.averageCase}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.appointments}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.openCases}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.waitingQueue}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100 tabular-nums">
          {staff.map((row) => (
            <tr key={row.staff_id}>
              <td className="px-3 py-2 text-ink-900">{row.display_name ?? row.staff_id}</td>
              <td className="px-3 py-2 text-right">{formatCount(row.cases)}</td>
              <td className="px-3 py-2 text-right">{formatMinutes(row.average_case_minutes)}</td>
              <td className="px-3 py-2 text-right">{formatCount(row.appointments)}</td>
              <td className="px-3 py-2 text-right">{formatCount(row.open_cases)}</td>
              <td className="px-3 py-2 text-right">{formatCount(row.waiting_queue)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
