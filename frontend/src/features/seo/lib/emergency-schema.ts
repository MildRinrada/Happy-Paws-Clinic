import type { SiteInfo } from '@/features/clinic/lib/load-site-info';
import type { ClinicProfile } from '@/features/clinic/types/clinic.types';
import { seoSite, sitePages } from '../content/seo';
import { absoluteUrl } from './absolute-url';
import { clinicId, clinicSchema } from './clinic-schema';
import { compact, type JsonLdObject } from './json-ld';

/**
 * The emergency page's line as a schema.org `EmergencyService` that is also `VeterinaryCare`, part
 * of the clinic (`parentOrganization` points at the clinic's `@id`, described on the same page by
 * `ClinicJsonLd`). `telephone` is the number the page puts first: the emergency entry, or the main
 * phone when there is none.
 *
 * The address, position, map, weekly hours and closures are the clinic's own, taken from
 * `clinicSchema` so the two cannot disagree. Nothing here claims round-the-clock hours: the
 * hours are the ones the clinic set.
 */
export function emergencySchema(profile: ClinicProfile | null, site: SiteInfo, telephone?: string): JsonLdObject {
  const clinic = clinicSchema(profile, site);
  const name = profile?.name || seoSite.name;

  return compact({
    '@context': 'https://schema.org',
    '@type': ['EmergencyService', 'VeterinaryCare'],
    '@id': absoluteUrl(`${sitePages.emergency.path}#emergency`),
    name: `${name} ${sitePages.emergency.name}`,
    url: absoluteUrl(sitePages.emergency.path),
    telephone,
    image: clinic.image,
    address: clinic.address,
    geo: clinic.geo,
    hasMap: clinic.hasMap,
    openingHoursSpecification: clinic.openingHoursSpecification,
    specialOpeningHoursSpecification: clinic.specialOpeningHoursSpecification,
    parentOrganization: { '@id': clinicId() },
  });
}
