import Link from 'next/link';
import type { SwitcherPet } from '../../../lib/pet-switcher';
import { PetAvatar } from '../../shared/pet-avatar';

/** One pet in the switcher: a pill with its initial and name, outlined in dark green while it is the one shown. */
export function PetChip({ pet }: { pet: SwitcherPet }) {
  return (
    <Link
      href={pet.href}
      aria-current={pet.current ? 'page' : undefined}
      className={`flex h-9 max-w-[10rem] items-center gap-2 rounded-full bg-white pl-1.5 pr-4 font-display text-[14.5px] transition ${
        pet.current ? 'border-2 border-brand-600 font-medium text-ink-900' : 'border border-ink-200 text-ink-700 hover:border-brand-300'
      } ${pet.deceased ? 'opacity-70' : ''}`}
    >
      <PetAvatar name={pet.name} photoUrl={pet.photoUrl} size="chip" tone={pet.tone} />
      <span className="truncate">{pet.name}</span>
    </Link>
  );
}
