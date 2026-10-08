import { EmptyState } from '@/components/ui/empty-state';
import { formatBaht } from '@/lib/format/money';
import { businessReport } from '../content/business-report';
import { formatCount } from '../lib/format-report';
import type { ServiceAnalytics } from '../types/analytics.types';

/** Services by revenue, with the medicine sold alongside each. */
export function ServiceTable({ services }: { services: ServiceAnalytics['services'] }) {
  const text = businessReport.services;

  if (services.length === 0) {
    return <EmptyState title={text.empty} />;
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-ink-100">
      <table className="w-full min-w-[640px] text-sm">
        <thead className="border-b border-ink-100 text-left text-ink-500">
          <tr>
            <th scope="col" className="px-3 py-2 font-medium">{text.columns.name}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.quantity}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.billed}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.revenue}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.medicine}</th>
            <th scope="col" className="px-3 py-2 text-right font-medium">{text.columns.afterMedicine}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-ink-100 tabular-nums">
          {services.map((service) => (
            <tr key={service.service_id ?? service.name}>
              <td className="px-3 py-2 text-ink-900">{service.name}</td>
              <td className="px-3 py-2 text-right">{formatCount(service.quantity)}</td>
              <td className="px-3 py-2 text-right text-ink-600">{formatBaht(service.billed_satang)}</td>
              <td className="px-3 py-2 text-right font-medium text-ink-900">{formatBaht(service.revenue_satang)}</td>
              <td className="px-3 py-2 text-right">{formatBaht(service.medicine_satang)}</td>
              <td className="px-3 py-2 text-right">{formatBaht(service.revenue_excluding_medicine_satang)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
