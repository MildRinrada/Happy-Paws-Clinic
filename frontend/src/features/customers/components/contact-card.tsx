import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatLongDate } from '@/lib/format/datetime';
import { customerDetail } from '../content/customer-detail';
import type { Customer } from '../types/customer.types';

/** Phone, email and when the record was opened. */
export function ContactCard({ customer }: { customer: Customer }) {
  const text = customerDetail.contact;
  const rows = [
    { label: text.phone, value: customer.phone },
    { label: text.email, value: customer.email ?? text.noEmail },
    { label: text.since, value: formatLongDate(customer.created_at) },
  ];

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody>
        <dl className="space-y-2.5 text-sm">
          {rows.map((row) => (
            <div key={row.label} className="flex justify-between gap-4">
              <dt className="text-ink-500">{row.label}</dt>
              <dd className="text-right font-medium text-ink-900">{row.value}</dd>
            </div>
          ))}
        </dl>
      </CardBody>
    </Card>
  );
}
