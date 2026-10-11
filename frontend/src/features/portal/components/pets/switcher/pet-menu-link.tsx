import Link from 'next/link';
import type { SwitcherPet } from '../../../lib/pet-switcher';
import { PetAvatar } from '../../shared/pet-avatar';

/** One pet in the "ทั้งหมด" panel: initial, name and nickname, marked while it is the one shown. */
export function PetMenuLink({ pet }: { pet: SwitcherPet }) {
  return (
    <li>
      <Link
        href={pet.href}
        aria-current={pet.current ? 'page' : undefined}
        className={`flex items-center gap-2.5 rounded-lg px-2 py-1.5 hover:bg-ink-50 ${pet.current ? 'bg-brand-50' : ''}`}
      >
        <PetAvatar name={pet.name} photoUrl={pet.photoUrl} size="chip" tone={pet.tone} />
        <span className="min-w-0">
          <span className={`block truncate font-display text-sm ${pet.current ? 'font-semibold text-brand-800' : 'text-ink-900'}`}>
            {pet.name}
          </span>
          {pet.nickname && <span className="block truncate text-xs text-ink-500">{pet.nickname}</span>}
        </span>
      </Link>
    </li>
  );
}
