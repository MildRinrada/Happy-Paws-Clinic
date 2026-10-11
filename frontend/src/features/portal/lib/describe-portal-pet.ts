import { petIdentity } from '@/features/pets/content/pet-identity';
import { petAge } from '@/features/pets/lib/pet-age';
import { portalPets } from '../content/portal-pets';
import type { MyPet } from '../types/portal.types';

/** "3 ปี", or "5 เดือน" for a pet under a year, with "ประมาณ" for an estimated birth date. */
function shortAge(pet: MyPet): string | undefined {
  if (!pet.birth_date) return undefined;

  const age = petAge(pet.birth_date);
  if (!age) return undefined;

  const text = portalPets.describe;
  const value = age.years > 0 ? text.years(age.years) : age.months > 0 ? text.months(age.months) : text.newborn;

  return pet.birth_date_estimated ? text.about(value) : value;
}

/**
 * The line under a pet's name: "สุนัข พันธุ์ปอมเมอเรเนียน เพศเมีย 3 ปี", from what the clinic
 * recorded. Anything unknown is left out. Worked out on the server, where today is the clinic's.
 */
export function describePortalPet(pet: MyPet): string {
  const text = portalPets.describe;

  return [
    pet.species,
    pet.breed && text.breed(pet.breed),
    pet.sex && pet.sex !== 'unknown' ? petIdentity.sex.options[pet.sex] : undefined,
    shortAge(pet),
    pet.neutered === true ? text.neutered : undefined,
  ]
    .filter(Boolean)
    .join(' ');
}
