import type { PetAvatarTone } from '../components/shared/pet-avatar';
import { PORTAL_HOME_HREF, portalPaths } from '../content/portal-navigation';
import type { MyPet } from '../types/portal.types';
import { describePortalPet } from './describe-portal-pet';
import { loadPetVaccines, type PetVaccine } from './load-pet-vaccines';
import { loadPetVisits, type NextVisitView, type PastVisitView } from './load-pet-visits';
import { loadPetWeights, type WeightPoint } from './load-pet-weights';
import { loadPortalHome } from './load-portal-home';
import { buildSwitcher, type SwitcherView } from './pet-switcher';
import { toneOfPet } from './pet-tone';

/** Everything one pet's page draws, already decided. The components only render it. */
export interface PetDetailView {
  pet: MyPet;
  tone: PetAvatarTone;
  /** "สุนัข พันธุ์ปอมเมอเรเนียน เพศเมีย 3 ปี". */
  description: string;
  weights: WeightPoint[];
  vaccines: PetVaccine[];
  /** False while only the due vaccines can be read; the card then says the given ones come later. */
  vaccinesComplete: boolean;
  recentVisits: PastVisitView[];
  nextVisit?: NextVisitView;
  /** Undefined for an account with one pet. */
  switcher?: SwitcherView;
  /** Where the phone's back arrow goes: the list, or home when the list would only lead back here. */
  backHref: string;
}

/**
 * One pet's page, for a pet on this account; `null` for any other id, which the page answers with
 * a 404, as the backend does. The pets come from `GET /portal/home`, which the layout has already read.
 */
export async function loadPetDetail(petId: string): Promise<PetDetailView | null> {
  const home = await loadPortalHome();
  const pet = home.pets.find((item) => item.id === petId);

  if (!pet) return null;

  const [weights, vaccines, visits] = await Promise.all([loadPetWeights(pet), loadPetVaccines(pet.id), loadPetVisits(pet.id)]);

  return {
    pet,
    tone: toneOfPet(home.pets.map((item) => item.id), pet.id),
    description: describePortalPet(pet),
    weights: weights.points,
    vaccines: vaccines.items,
    vaccinesComplete: vaccines.complete,
    recentVisits: visits.recent,
    nextVisit: visits.next,
    switcher: buildSwitcher(home.pets, pet.id),
    backHref: home.pets.length > 1 ? portalPaths.pets : PORTAL_HOME_HREF,
  };
}
