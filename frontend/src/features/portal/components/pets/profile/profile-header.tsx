import { portalPets } from '../../../content/portal-pets';
import type { MyPet } from '../../../types/portal.types';
import { PetAvatar, type PetAvatarTone } from '../../shared/pet-avatar';

interface ProfileHeaderProps {
  pet: MyPet;
  tone: PetAvatarTone;
  description: string;
}

/**
 * The mint band at the top of a pet's card: photo or initial, name, what the clinic recorded about
 * it, and the nickname. Side by side on a phone, centred from 1024px, as the two designs draw it.
 */
export function ProfileHeader({ pet, tone, description }: ProfileHeaderProps) {
  return (
    <div className="flex items-center gap-4 bg-brand-100 px-5 py-5 lg:flex-col lg:gap-0 lg:px-5 lg:pb-5 lg:pt-5 lg:text-center">
      <PetAvatar name={pet.name} photoUrl={pet.photo_url} size="hero" tone={tone === 'mint' ? 'white' : tone} ring />

      <div className="min-w-0 lg:mt-3">
        <h2 className="break-words font-display text-[22px] font-semibold leading-7 text-brand-900 lg:text-[28px] lg:leading-9">
          {pet.name}
        </h2>
        {description && <p className="mt-0.5 text-[14.5px] leading-[22px] text-brand-800 lg:mt-1.5 lg:text-[13.5px]">{description}</p>}
        {pet.nickname && (
          <p className="text-[13px] leading-5 text-brand-700 lg:mt-1.5 lg:text-xs">{portalPets.nickname(pet.nickname)}</p>
        )}
        {pet.is_deceased && (
          <p className="mt-1.5 inline-block rounded-full bg-white/70 px-2.5 text-xs leading-6 text-ink-700">{portalPets.deceased}</p>
        )}
      </div>
    </div>
  );
}
