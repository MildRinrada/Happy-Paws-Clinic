import Link from 'next/link';
import { portalHome } from '../../content/portal-home';
import { portalPaths } from '../../content/portal-navigation';
import type { MyPet } from '../../types/portal.types';
import { PetAvatar, type PetAvatarTone } from '../shared/pet-avatar';

/** One pet on the home page: photo or initial, name, species and the latest weight. The tile opens the pet's page. */
export function PetTile({ pet, tone }: { pet: MyPet; tone: PetAvatarTone }) {
  const text = portalHome.pets;
  const facts = [pet.species, pet.latest_weight && text.weight(pet.latest_weight.weight_kg)].filter(Boolean).join(' ');

  return (
    <Link
      href={portalPaths.pet(pet.id)}
      className="block h-full rounded-2xl border border-ink-200 bg-white px-3.5 pb-4 pt-3.5 transition hover:border-brand-300"
    >
      <PetAvatar name={pet.name} photoUrl={pet.photo_url} size="tile" tone={tone} />
      <p className="mt-2 truncate font-display text-base font-semibold leading-[22px] text-ink-900">{pet.name}</p>
      <p className="truncate text-[13px] leading-[18px] text-ink-600">
        {facts}
        {pet.is_deceased && ` (${text.deceased})`}
      </p>
    </Link>
  );
}
