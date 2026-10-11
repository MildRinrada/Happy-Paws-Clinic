import type { PetAvatarTone } from '../components/shared/pet-avatar';
import { portalPaths } from '../content/portal-navigation';
import { portalPets } from '../content/portal-pets';
import type { MyPet, VaccinationDue } from '../types/portal.types';
import { alivePetsFirst } from './order-pets';
import { toneOfPet } from './pet-tone';

/** A short warning under a pet's name in the list. */
export interface PetListHint {
  tone: 'alert' | 'warn';
  text: string;
}

/** One card in the list of pets, already worded. Plain data, so the list can filter it in the browser. */
export interface PetListItem {
  id: string;
  name: string;
  nickname?: string | null;
  photoUrl?: string | null;
  tone: PetAvatarTone;
  /** "สุนัข ปอมเมอเรเนียน 4.2 กก.". */
  line: string;
  hint?: PetListHint;
  deceased: boolean;
  href: string;
}

/** The most urgent vaccine of a pet, worded, with how many more are due. */
function vaccineHint(due: VaccinationDue[]): PetListHint | undefined {
  const urgent = (item: VaccinationDue) => (item.status === 'overdue' ? 0 : 1);
  const [first, ...rest] = [...due].sort((a, b) => urgent(a) - urgent(b) || a.next_due_on.localeCompare(b.next_due_on));
  if (!first) return undefined;

  const text = portalPets.list.hint;
  const overdue = first.status === 'overdue';
  const line = overdue ? text.overdue(first.vaccine_name) : text.dueSoon(first.vaccine_name);

  return { tone: overdue ? 'alert' : 'warn', text: rest.length > 0 ? text.more(line, rest.length) : line };
}

/**
 * The account's pets as list cards, living pets first. `vaccinations_due` comes from
 * `GET /portal/home` and covers living pets only.
 */
export function petListItems(pets: MyPet[], due: VaccinationDue[]): PetListItem[] {
  const ids = pets.map((pet) => pet.id);

  return alivePetsFirst(pets).map((pet) => ({
    id: pet.id,
    name: pet.name,
    nickname: pet.nickname,
    photoUrl: pet.photo_url,
    tone: toneOfPet(ids, pet.id),
    line: [pet.species, pet.breed, pet.latest_weight && portalPets.list.weight(pet.latest_weight.weight_kg)]
      .filter(Boolean)
      .join(' '),
    hint: pet.is_deceased ? undefined : vaccineHint(due.filter((item) => item.pet_id === pet.id)),
    deceased: pet.is_deceased,
    href: portalPaths.pet(pet.id),
  }));
}
