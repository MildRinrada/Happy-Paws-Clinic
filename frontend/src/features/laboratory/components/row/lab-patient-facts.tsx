import Link from 'next/link';
import { consultationPaths } from '@/features/consultations/lib/consultation-paths';
import { petPaths } from '@/features/pets/lib/pet-paths';
import { labPage } from '../../content/lab-page';
import type { LabRow } from '../../lib/load-lab-worklist';

/**
 * Who the request is about and who sent it: HN, owner and requesting vet, each shown only when the
 * session could read it, with the links back to the case and the pet's file.
 */
export function LabPatientFacts({ row, canOpenCase }: { row: LabRow; canOpenCase: boolean }) {
  const { request, pet, ownerName, vetName } = row;
  const text = labPage.patient;
  const facts: { label: string; value: string }[] = [];
  if (pet) facts.push({ label: 'HN', value: pet.hospitalNumber });
  if (ownerName) facts.push({ label: text.owner, value: ownerName });
  if (vetName) facts.push({ label: text.vet, value: vetName });

  return (
    <div className="flex flex-wrap items-baseline justify-between gap-x-6 gap-y-2 text-sm">
      {facts.length > 0 && (
        <dl className="flex flex-wrap gap-x-5 gap-y-1">
          {facts.map((fact) => (
            <div key={fact.label} className="flex gap-1.5">
              <dt className="text-ink-500">{fact.label}</dt>
              <dd className="font-medium text-ink-800">{fact.value}</dd>
            </div>
          ))}
        </dl>
      )}
      <div className="flex flex-wrap gap-4">
        {canOpenCase && request.consultation_id && (
          <Link href={consultationPaths.detail(request.consultation_id)} className="font-medium text-brand-700 hover:underline">
            {labPage.request.openCase}
          </Link>
        )}
        {pet && (
          <Link href={petPaths.detail(request.pet_id)} className="font-medium text-brand-700 hover:underline">
            {labPage.request.openPet}
          </Link>
        )}
      </div>
    </div>
  );
}
