import { petDetail } from '../../../content/pet-detail';
import type { SwitcherView } from '../../../lib/pet-switcher';
import { PetChip } from './pet-chip';
import { PetSearchMenu } from './pet-search-menu';
import { PetSelect } from './pet-select';

/**
 * The pets at the top right of a pet's page, from 1024px: a chip each for up to five, or a few
 * chips (the current pet always among them) and "ทั้งหมด N ตัว" for more. The chips wrap rather
 * than push past the page's edge.
 */
export function PetSwitcher({ view }: { view: SwitcherView }) {
  return (
    <nav aria-label={petDetail.switcher.label} className="flex min-w-0 flex-wrap items-center justify-end gap-2">
      <ul className="flex min-w-0 flex-wrap items-center justify-end gap-2">
        {view.chips.map((pet) => (
          <li key={pet.id} className="min-w-0">
            <PetChip pet={pet} />
          </li>
        ))}
      </ul>
      {view.all &&
        (view.all.mode === 'search' ? (
          <PetSearchMenu count={view.all.count} pets={view.all.pets} />
        ) : (
          <PetSelect count={view.all.count} pets={view.all.pets} />
        ))}
    </nav>
  );
}
