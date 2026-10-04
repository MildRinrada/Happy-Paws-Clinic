import Link from 'next/link';
import { petPaths } from '@/features/pets/lib/pet-paths';
import type { PetDetail } from '@/features/pets/types/pet.types';
import { prescriptionsPage } from '../content/prescriptions-page';

/** Whose medicines these are, and what they are allergic to, above the history. */
export function PetBanner({ detail }: { detail: PetDetail }) {
  const text = prescriptionsPage.pet;
  const { pet, allergies } = detail;

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-3 text-sm">
        <span className="font-display text-lg font-semibold text-ink-900">{pet.name}</span>
        <span className="rounded-md bg-ink-100 px-2 py-1 font-mono text-xs text-ink-700">{pet.hospital_number}</span>
        <span className="text-ink-600">{[pet.species, pet.breed].filter(Boolean).join(' / ')}</span>
        <Link href={petPaths.detail(pet.id)} className="text-brand-700 hover:underline">
          {text.profile}
        </Link>
      </div>

      {allergies.length > 0 && (
        <p role="note" className="rounded-lg border border-red-200 bg-red-50 px-3.5 py-2 text-sm font-medium text-red-800">
          {text.allergies(allergies.map((allergy) => allergy.substance).join(', '))}
        </p>
      )}
    </div>
  );
}
