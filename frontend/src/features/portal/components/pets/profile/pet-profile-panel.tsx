import { portalPets } from '../../../content/portal-pets';
import type { MyPet } from '../../../types/portal.types';
import type { PetAvatarTone } from '../../shared/pet-avatar';
import { AllergyRow } from './allergy-row';
import { ProfileActions } from './profile-actions';
import { ProfileFacts } from './profile-facts';
import { ProfileHeader } from './profile-header';

interface PetProfilePanelProps {
  pet: MyPet;
  tone: PetAvatarTone;
  description: string;
}

/** A pet's profile card: who it is, its numbers and the owner's notes, and what it is allergic to. */
export function PetProfilePanel({ pet, tone, description }: PetProfilePanelProps) {
  const text = portalPets.facts;

  return (
    <article className="overflow-hidden rounded-2xl border border-ink-200 bg-white">
      <ProfileHeader pet={pet} tone={tone} description={description} />
      <ProfileFacts
        facts={[
          { label: text.hospitalNumber, value: pet.hospital_number },
          { label: text.microchip, value: pet.microchip_number },
          { label: text.diet, value: pet.diet },
          { label: text.behaviour, value: pet.behaviour },
        ]}
      />
      <AllergyRow allergies={pet.allergies} />
      <ProfileActions pet={pet} />
    </article>
  );
}
