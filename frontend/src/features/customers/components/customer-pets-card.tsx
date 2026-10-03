import { Card, CardHeader } from '@/components/ui/card';
import { AddPetPanel } from '@/features/pets/components/add-pet-panel';
import { PetListItem } from '@/features/pets/components/pet-list-item';
import type { PetSummary } from '@/features/pets/types/pet.types';
import { customerDetail } from '../content/customer-detail';

interface CustomerPetsCardProps {
  customerId: string;
  pets: PetSummary[];
  canAdd: boolean;
  /** Opens the add form at once: a customer registered a moment ago is here to add a pet. */
  addFirst: boolean;
}

/** The customer's animals, and the form to register another. */
export function CustomerPetsCard({ customerId, pets, canAdd, addFirst }: CustomerPetsCardProps) {
  const text = customerDetail.pets;

  return (
    <Card>
      <CardHeader title={`${text.title} (${pets.length})`} />
      {pets.length === 0 ? (
        <p className="px-5 py-4 text-sm text-ink-500">{text.empty}</p>
      ) : (
        <ul className="divide-y divide-ink-100">
          {pets.map((pet) => (
            <PetListItem key={pet.id} pet={pet} />
          ))}
        </ul>
      )}
      {canAdd && (
        <div className="border-t border-ink-100">
          <AddPetPanel customerId={customerId} startOpen={addFirst && pets.length === 0} />
        </div>
      )}
    </Card>
  );
}
