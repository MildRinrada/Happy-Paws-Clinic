import Link from 'next/link';
import { portalRecords } from '../../content/portal-records';
import { recordsHref } from '../../lib/portal-links';
import type { MyPet } from '../../types/portal.types';

/** One pill per pet; the chosen one is filled. Plain links, so it needs no script. */
export function PetPicker({ pets, selectedId }: { pets: MyPet[]; selectedId: string }) {
  return (
    <nav aria-label={portalRecords.pickerLabel}>
      <ul className="flex flex-wrap gap-2">
        {pets.map((pet) => {
          const selected = pet.id === selectedId;

          return (
            <li key={pet.id}>
              <Link
                href={recordsHref(pet.id)}
                aria-current={selected ? 'page' : undefined}
                className={`inline-flex h-9 items-center rounded-full px-4 text-sm font-medium transition ${
                  selected ? 'bg-site-teal text-white' : 'bg-white text-ink-700 shadow-sm hover:text-site-teal'
                }`}
              >
                {pet.name}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
