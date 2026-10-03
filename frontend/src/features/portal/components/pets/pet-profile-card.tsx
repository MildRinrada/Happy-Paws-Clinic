import Link from 'next/link';
import { formatLongDate } from '@/lib/format/datetime';
import { portalPaths } from '../../content/portal-navigation';
import { portalPets } from '../../content/portal-pets';
import { recordsHref } from '../../lib/portal-links';
import type { MyPet } from '../../types/portal.types';
import { PetAvatar } from '../shared/pet-avatar';
import { AllergyTags } from './allergy-tags';
import { PetFacts } from './pet-facts';

/** One pet: who it is, the clinic's facts, the owner's notes, allergies, and its records. */
export function PetProfileCard({ pet }: { pet: MyPet }) {
  const text = portalPets.facts;

  return (
    <article className="space-y-4 rounded-2xl bg-white p-5 shadow-sm">
      <div className="flex items-center gap-4">
        <PetAvatar name={pet.name} photoUrl={pet.photo_url} size="lg" />
        <div className="min-w-0">
          <h2 className="truncate text-lg font-semibold text-ink-900">
            {pet.name}
            {pet.is_deceased && <span className="ml-2 text-sm font-normal text-site-muted">({portalPets.deceased})</span>}
          </h2>
          <p className="text-sm text-site-muted">{[pet.species, pet.breed].filter(Boolean).join(' ')}</p>
          {pet.nickname && <p className="text-sm text-ink-700">{portalPets.nickname(pet.nickname)}</p>}
        </div>
      </div>

      <PetFacts
        facts={[
          { label: text.hospitalNumber, value: pet.hospital_number },
          { label: text.microchip, value: pet.microchip_number },
          {
            label: text.weight,
            value: pet.latest_weight && text.weightValue(pet.latest_weight.weight_kg, formatLongDate(pet.latest_weight.recorded_at)),
          },
          { label: text.diet, value: pet.diet },
          { label: text.behaviour, value: pet.behaviour },
        ]}
      />

      <AllergyTags allergies={pet.allergies} />

      <div className="flex flex-wrap gap-x-6 gap-y-2">
        {!pet.is_deceased && (
          <Link href={portalPaths.editPet(pet.id)} className="text-sm font-semibold text-site-teal hover:underline">
            {portalPets.edit}
          </Link>
        )}
        {pet.may_view_history && (
          <Link href={recordsHref(pet.id)} className="text-sm font-semibold text-site-teal hover:underline">
            {portalPets.records}
          </Link>
        )}
      </div>
    </article>
  );
}
