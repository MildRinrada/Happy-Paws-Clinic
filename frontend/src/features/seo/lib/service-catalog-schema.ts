import { serviceCard } from '@/features/service-catalog/content/services-page';
import { priceRange } from '@/features/service-catalog/lib/service-price';
import type { ServiceMenu } from '@/features/service-catalog/types/service-menu.types';
import type { PublicService } from '@/features/service-catalog/types/service.types';
import { sitePages } from '../content/seo';
import { absoluteUrl } from './absolute-url';
import { clinicId } from './clinic-schema';
import { compact, type JsonLdObject } from './json-ld';

/** Satang as the decimal string schema.org wants: 50000 is "500.00". */
const price = (satang: number) => (satang / 100).toFixed(2);

/**
 * One service as an `Offer`. A single price is `price`; a price that depends on species or weight
 * is a `PriceSpecification` from the lowest to the highest. A service with no price on the menu
 * gets no price at all rather than a misleading zero.
 */
function offer(service: PublicService, categoryName: string, provider: JsonLdObject): JsonLdObject {
  const { min, max } = priceRange(service);
  const priced = max > 0;

  return compact({
    '@type': 'Offer',
    url: absoluteUrl(serviceCard.bookHref(service.id)),
    priceCurrency: priced ? 'THB' : undefined,
    price: priced && min === max ? price(min) : undefined,
    priceSpecification:
      priced && min !== max
        ? { '@type': 'PriceSpecification', priceCurrency: 'THB', minPrice: price(min), maxPrice: price(max) }
        : undefined,
    itemOffered: { '@type': 'Service', name: service.name, category: categoryName, provider },
  });
}

/**
 * The bookable services as a schema.org `OfferCatalog`, one nested catalog per category. Every
 * name and price is the clinic's own, from `GET /services`.
 */
export function serviceCatalogSchema(menu: ServiceMenu, clinicName: string): JsonLdObject {
  const provider = { '@type': 'VeterinaryCare', '@id': clinicId(), name: clinicName };

  return {
    '@context': 'https://schema.org',
    '@type': 'OfferCatalog',
    name: sitePages.services.name,
    url: absoluteUrl(sitePages.services.path),
    itemListElement: menu.categories.map((category) => ({
      '@type': 'OfferCatalog',
      name: category.name,
      itemListElement: category.services.map((service) => offer(service, category.name, provider)),
    })),
  };
}
