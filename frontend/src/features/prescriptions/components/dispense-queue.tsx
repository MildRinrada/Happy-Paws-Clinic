import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatDateTime } from '@/lib/format/datetime';
import { prescriptionsPage } from '../content/prescriptions-page';
import type { QueueRow } from '../lib/load-prescriptions-view';
import { prescriptionPaths } from '../lib/prescription-paths';

/**
 * Prescriptions in the caller's branches not yet handed over. Dispensing is done from the pet's
 * history rather than here, so the counter sees the rest of the patient's medicines first.
 */
export function DispenseQueue({ rows }: { rows: QueueRow[] }) {
  const text = prescriptionsPage;

  return (
    <Card>
      <CardHeader title={text.queue.heading} />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-500">{text.queue.hint}</p>

        {rows.length === 0 ? (
          <p className="text-sm text-ink-500">{text.queue.empty}</p>
        ) : (
          <ul className="divide-y divide-ink-100 text-sm">
            {rows.map(({ prescription, pet }) => (
              <li key={prescription.id} className="flex flex-wrap items-start justify-between gap-3 py-2.5">
                <div className="min-w-0 space-y-0.5">
                  <p className="flex flex-wrap items-center gap-2">
                    <span className="font-medium text-ink-900">{prescription.drug_name}</span>
                    {prescription.duplicate_of.length > 0 && <Badge tone="red">{text.duplicate.badge}</Badge>}
                  </p>
                  <p className="text-ink-700">
                    {text.item.line(prescription.dose, prescription.frequency, prescription.duration_days, prescription.quantity)}
                  </p>
                  <p className="text-ink-500">{text.queue.prescribedAt(formatDateTime(prescription.prescribed_at))}</p>
                </div>
                <div className="text-right">
                  <Link href={prescriptionPaths.forPet(prescription.pet_id)} className="font-medium text-brand-700 hover:underline">
                    {pet?.name ?? text.search.open}
                  </Link>
                  {pet && <p className="font-mono text-xs text-ink-500">{pet.hospital_number}</p>}
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardBody>
    </Card>
  );
}
