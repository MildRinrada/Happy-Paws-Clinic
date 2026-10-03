import Link from 'next/link';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { formatLongDate, formatTime } from '@/lib/format/datetime';
import { appointmentCalendar, appointmentDetail } from '../../content/appointment-calendar';
import type { CalendarItem } from '../../types/calendar.types';

/** What the appointment is: when, whose, what for, with whom. */
export function DetailRows({ item }: { item: CalendarItem }) {
  const { appointment } = item;
  const text = appointmentDetail.fields;

  const rows = [
    {
      label: text.time,
      value: `${formatLongDate(appointment.starts_at)} ${formatTime(appointment.starts_at)} - ${formatTime(appointment.ends_at)} น.`,
    },
    { label: text.service, value: item.serviceName ?? appointmentCalendar.unnamedService },
    { label: text.vet, value: item.vetName ?? appointmentCalendar.noVet },
    { label: text.type, value: appointmentDetail.types[appointment.type] ?? appointment.type },
    appointment.reason ? { label: text.reason, value: appointment.reason } : null,
    appointment.cancellation_reason ? { label: text.cancelReason, value: appointment.cancellation_reason } : null,
  ].filter((row): row is { label: string; value: string } => row !== null);

  return (
    <dl className="space-y-2 text-sm">
      <div className="flex justify-between gap-4">
        <dt className="text-ink-500">{text.owner}</dt>
        <dd className="text-right">
          <Link href={customerPaths.detail(appointment.customer_id)} className="font-medium text-brand-700 hover:underline">
            {item.customerName ?? appointmentDetail.openCustomer}
          </Link>
          {item.customerPhone && <span className="block text-ink-600">{item.customerPhone}</span>}
        </dd>
      </div>
      {rows.map((row) => (
        <div key={row.label} className="flex justify-between gap-4">
          <dt className="shrink-0 text-ink-500">{row.label}</dt>
          <dd className="text-right font-medium text-ink-900">{row.value}</dd>
        </div>
      ))}
    </dl>
  );
}
