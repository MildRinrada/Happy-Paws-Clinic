import Link from 'next/link';
import { Badge } from '@/components/ui/badge';
import { petPaths } from '../lib/pet-paths';
import { customerDetail } from '@/features/customers/content/customer-detail';
import type { PetSummary } from '../types/pet.types';

/** One patient on an owner's page: name, what it is, and the HN the counter reads out. */
export function PetListItem({ pet }: { pet: PetSummary }) {
  const text = customerDetail.pets;

  return (
    <li className="flex flex-wrap items-center justify-between gap-3 px-5 py-3">
      <div className="min-w-0">
        <p className="font-medium text-ink-900">
          <Link href={petPaths.detail(pet.id)} className="text-brand-700 hover:underline">
            {pet.name}
          </Link>
          {pet.is_deceased && (
            <span className="ml-2">
              <Badge>{text.deceased}</Badge>
            </span>
          )}
        </p>
        <p className="text-sm text-ink-600">
          {[pet.species, pet.breed, pet.microchip_number && `${text.microchip} ${pet.microchip_number}`]
            .filter(Boolean)
            .join(' / ')}
        </p>
      </div>
      <span className="rounded-md bg-ink-100 px-2 py-1 font-mono text-xs text-ink-700">{pet.hospital_number}</span>
    </li>
  );
}
