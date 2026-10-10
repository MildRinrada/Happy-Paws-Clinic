import type { PublicService } from '@/features/service-catalog/types/service.types';
import type { Species } from '../types/vaccine-schedule.types';

/** The service category the contract gives vaccinations (`GET /services/categories`). */
const VACCINE_CATEGORY = 'vaccine';

const BOOK_PATH = '/book';

/** Words that mark a service as for one species, in its name or its price tiers. */
const speciesWords: Record<Species, string[]> = {
  dog: ['สุนัข', 'หมา', 'dog'],
  cat: ['แมว', 'cat'],
};

function isFor(service: PublicService, species: Species): boolean {
  const haystack = [service.name, ...service.price_tiers.map((tier) => tier.species ?? '')].join(' ').toLowerCase();
  return speciesWords[species].some((word) => haystack.includes(word));
}

function bookHref(service?: PublicService): string {
  return service ? `${BOOK_PATH}?service=${encodeURIComponent(service.id)}` : BOOK_PATH;
}

/**
 * Where "จองฉีดวัคซีน" goes for each species: the booking page with a vaccination service picked,
 * the one named for that species when there is one, otherwise the first vaccination service. With
 * no vaccination service open for online booking, the booking page with nothing picked.
 */
export function vaccineBookLinks(services: PublicService[]): Record<Species, string> {
  const vaccines = services.filter((service) => service.category_code === VACCINE_CATEGORY);
  const pick = (species: Species) => vaccines.find((service) => isFor(service, species)) ?? vaccines[0];

  return { dog: bookHref(pick('dog')), cat: bookHref(pick('cat')) };
}
