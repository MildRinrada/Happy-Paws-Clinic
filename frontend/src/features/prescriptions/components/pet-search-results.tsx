import Link from 'next/link';
import { EmptyState } from '@/components/ui/empty-state';
import type { PetSummary } from '@/features/pets/types/pet.types';
import { prescriptionsPage } from '../content/prescriptions-page';
import { prescriptionPaths } from '../lib/prescription-paths';

/** The pets matching a search. Each opens that pet's medication history. */
export function PetSearchResults({ query, pets }: { query: string; pets: PetSummary[] }) {
  const text = prescriptionsPage.search;

  return (
    <section className="space-y-3">
      <h2 className="text-sm font-medium text-ink-500">{text.resultsHeading(query)}</h2>

      {pets.length === 0 ? (
        <EmptyState title={text.none} />
      ) : (
        <ul className="divide-y divide-ink-100 rounded-xl border border-ink-200 bg-white text-sm">
          {pets.map((pet) => (
            <li key={pet.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-2.5">
              <div className="min-w-0">
                <Link href={prescriptionPaths.forPet(pet.id)} className="font-medium text-brand-700 hover:underline">
                  {pet.name}
                </Link>
                <span className="ml-2 text-ink-600">{[pet.species, pet.breed].filter(Boolean).join(' / ')}</span>
              </div>
              <span className="rounded-md bg-ink-100 px-2 py-1 font-mono text-xs text-ink-700">{pet.hospital_number}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
