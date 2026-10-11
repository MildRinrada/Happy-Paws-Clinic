import type { PetListItem } from '../../../lib/pet-list-items';
import { PetListCard } from './pet-list-card';

/** Pet cards one under another on a phone, two to a row from 640px. */
export function PetCardGrid({ pets }: { pets: PetListItem[] }) {
  return (
    <ul className="grid gap-2.5 sm:grid-cols-2">
      {pets.map((pet) => (
        <li key={pet.id} className="min-w-0">
          <PetListCard pet={pet} />
        </li>
      ))}
    </ul>
  );
}
