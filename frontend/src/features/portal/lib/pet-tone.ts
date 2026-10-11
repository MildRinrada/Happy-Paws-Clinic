import type { PetAvatarTone } from '../components/shared/pet-avatar';

/**
 * The colour of a pet's circle when it has no photo: yellow and mint in turn, by the pet's place
 * in the account's own list (`GET /portal/pets`), so one pet keeps one colour on every page.
 */
export function petTone(index: number): PetAvatarTone {
  return index % 2 === 0 ? 'sun' : 'mint';
}

/** The tone of one pet, looked up in the account's list. */
export function toneOfPet(petIds: string[], petId: string): PetAvatarTone {
  return petTone(Math.max(0, petIds.indexOf(petId)));
}
