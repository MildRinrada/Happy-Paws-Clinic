import Link from 'next/link';
import { formatDay } from '@/features/inventory/lib/format-day';
import { petPaths } from '@/features/pets/lib/pet-paths';
import { dueBackPage } from '../../content/due-back-page';
import type { DueBackPet } from '../../types/due-back.types';

/** The customer's pets on the list, each linked, with the doses that are overdue. */
export function DueBackPets({ pets }: { pets: DueBackPet[] }) {
  return (
    <ul className="space-y-0.5 text-sm">
      {pets.map((pet) => (
        <li key={pet.pet_id} className="text-ink-700">
          <Link href={petPaths.detail(pet.pet_id)} className="font-medium text-brand-700 hover:underline">
            {pet.name}
          </Link>
          <span className="ml-1 text-ink-500">({pet.species})</span>
          {pet.overdue_vaccines.length > 0 && (
            <span className="ml-2 text-ink-600">
              {pet.overdue_vaccines.map((dose) => dueBackPage.vaccine(dose.vaccine_name, formatDay(dose.due_on))).join(', ')}
            </span>
          )}
        </li>
      ))}
    </ul>
  );
}
