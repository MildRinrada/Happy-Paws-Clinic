import { portalPets } from '../../../content/portal-pets';
import type { PetListItem } from '../../../lib/pet-list-items';
import { ChevronIcon } from '../chevron-icon';
import { PetCardGrid } from './pet-card-grid';

interface DeceasedPetsProps {
  pets: PetListItem[];
  /** Opened by a search that found one of them. */
  open: boolean;
}

/**
 * "น้องที่จากไปแล้ว", folded away under the living pets so they do not crowd the list, and one tap
 * away. A <details> element, so it opens with no script.
 */
export function DeceasedPets({ pets, open }: DeceasedPetsProps) {
  if (pets.length === 0) return null;

  return (
    <details open={open} className="group">
      <summary className="flex cursor-pointer list-none items-center gap-1.5 py-1 font-display text-[15px] font-medium text-ink-700 [&::-webkit-details-marker]:hidden">
        <ChevronIcon direction="right" className="size-4 transition group-open:rotate-90" />
        {portalPets.list.deceasedGroup(pets.length)}
      </summary>
      <div className="mt-2.5">
        <PetCardGrid pets={pets} />
      </div>
    </details>
  );
}
