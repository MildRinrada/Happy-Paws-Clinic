import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { petPaths } from '@/features/pets/lib/pet-paths';
import { formatDateTime } from '@/lib/format/datetime';
import { consultationPage } from '../content/consultation-page';
import type { ConsultationView } from '../lib/load-consultation-view';

/**
 * Who is on the table: the patient, the owner, when the case opened, and in red what the patient
 * must not be given. The allergy line is the first thing under the name, on every case.
 */
export function ConsultationHeader({ view }: { view: ConsultationView }) {
  const { consultation, pet, ownerName } = view;
  const text = consultationPage;
  const allergies = pet?.allergies ?? [];

  return (
    <header className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <h1 className="flex flex-wrap items-center gap-2 font-display text-2xl font-semibold text-ink-900">
            {pet?.pet.name ?? consultation.pet_id}
            {pet && <span className="rounded-md bg-ink-100 px-2 py-0.5 font-mono text-sm font-normal text-ink-700">{pet.pet.hospital_number}</span>}
            <Badge tone={consultation.status === 'draft' ? 'amber' : 'green'}>{text.status[consultation.status]}</Badge>
          </h1>
          <p className="text-sm text-ink-600">
            {[pet?.pet.species, pet?.pet.breed, pet?.latest_weight && `${pet.latest_weight.weight_kg} กก.`].filter(Boolean).join(' / ')}
          </p>
          <p className="text-sm text-ink-600">
            {text.owner}{' '}
            <Link href={customerPaths.detail(consultation.customer_id)} className="font-medium text-brand-700 hover:underline">
              {ownerName ?? consultation.customer_id}
            </Link>
            {' / '}
            {consultation.closed_at ? text.closedAt(formatDateTime(consultation.closed_at)) : text.openedAt(formatDateTime(consultation.opened_at))}
          </p>
        </div>
        <Link href={petPaths.detail(consultation.pet_id)} className="text-sm font-medium text-brand-700 hover:underline">
          {text.petPage}
        </Link>
      </div>

      {allergies.length > 0 && (
        <p role="note" className="rounded-lg border border-red-300 bg-red-50 px-3.5 py-2.5 text-sm font-semibold text-red-800">
          {text.allergy(allergies.map((allergy) => allergy.substance).join(', '))}
        </p>
      )}
    </header>
  );
}
