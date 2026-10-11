import Link from 'next/link';
import { portalPets } from '../../../content/portal-pets';
import type { PetListItem } from '../../../lib/pet-list-items';
import { PetAvatar } from '../../shared/pet-avatar';
import { ChevronIcon } from '../chevron-icon';
import { PetStatusHint } from './pet-status-hint';

/** One pet in the list: photo or initial, name, species and weight, and a warning when a vaccine is due. The whole card opens its page. */
export function PetListCard({ pet }: { pet: PetListItem }) {
  return (
    <Link
      href={pet.href}
      aria-label={portalPets.list.open(pet.name)}
      className="flex items-center gap-3 rounded-2xl border border-ink-200 bg-white py-3 pl-3 pr-2.5 transition hover:border-brand-300"
    >
      <PetAvatar name={pet.name} photoUrl={pet.photoUrl} size="tile" tone={pet.tone} />

      <div className="min-w-0 flex-1">
        <p className="truncate font-display text-base font-semibold leading-6 text-ink-900">
          {pet.name}
          {pet.nickname && <span className="ml-1.5 text-[13px] font-normal text-ink-500">({pet.nickname})</span>}
        </p>
        {pet.line && <p className="truncate text-[13px] leading-5 text-ink-600">{pet.line}</p>}
        {pet.hint && <PetStatusHint hint={pet.hint} />}
        {pet.deceased && <p className="text-[12.5px] leading-5 text-ink-500">{portalPets.deceased}</p>}
      </div>

      <ChevronIcon direction="right" className="size-5 shrink-0 text-ink-400" />
    </Link>
  );
}
