import type { SiteInfo } from '@/features/clinic/lib/load-site-info';
import { logoSrcOf } from '@/features/clinic/lib/brand-image-src';
import type { Branch, ClinicProfile } from '@/features/clinic/types/clinic.types';
import type { ClinicContact } from '@/features/clinic/types/contact.types';
import { ogImage, seoSite } from '../content/seo';
import { absoluteUrl } from './absolute-url';
import { compact, type JsonLdObject } from './json-ld';
import { closuresSchema, weeklyHoursSchema } from './opening-hours-schema';

/** The `@id` other structured data uses to point at the clinic. */
export const clinicId = () => absoluteUrl('/#clinic');

/**
 * The address is one line of text in the API, so it goes in as the street address. The country is
 * not a clinic setting: the whole product is Thai (baht, PDPA), so it is always TH.
 */
const postalAddress = (text?: string) =>
  text ? { '@type': 'PostalAddress', streetAddress: text, addressCountry: 'TH' } : undefined;

const geo = (branch?: Branch) =>
  branch?.latitude !== undefined && branch.longitude !== undefined
    ? { '@type': 'GeoCoordinates', latitude: branch.latitude, longitude: branch.longitude }
    : undefined;

const isWebLink = (url?: string): url is string => Boolean(url && /^https?:\/\//i.test(url));

/** LINE, Facebook, Instagram and website links of the whole clinic: the profiles that are the clinic. */
function profileLinks(site: SiteInfo): string[] {
  const kinds: ClinicContact['kind'][] = ['line', 'facebook', 'instagram', 'website'];

  return site.contacts
    .filter((contact) => kinds.includes(contact.kind) && !contact.branch_id)
    .map((contact) => contact.url)
    .filter(isWebLink);
}

/** One branch, when the clinic has more than one, as a department of the clinic. */
function branchSchema(clinicName: string, branch: Branch, site: SiteInfo): JsonLdObject {
  const phone = site.contacts.find((contact) => contact.kind === 'phone' && contact.branch_id === branch.id);

  return compact({
    '@type': 'VeterinaryCare',
    name: `${clinicName} ${branch.name}`,
    address: postalAddress(branch.address),
    geo: geo(branch),
    telephone: phone?.value,
  });
}

/**
 * The clinic as a schema.org `VeterinaryCare` (a kind of LocalBusiness), built only from what the
 * clinic has set in the API: name and logo (`GET /clinic`), branches with address and position
 * (`GET /clinic/branches`), weekly hours and closures (`GET /clinic/hours`), and phone, email,
 * emergency line, map and social profiles (`GET /clinic/contacts`). Anything not set is left out.
 *
 * With one branch, its address and position are the clinic's. With several, each is listed as a
 * department, and the clinic's own address is its clinic-wide map entry, if any.
 */
export function clinicSchema(profile: ClinicProfile | null, site: SiteInfo): JsonLdObject {
  const name = profile?.name || seoSite.name;
  const single = site.branches.length === 1 ? site.branches[0] : undefined;
  const clinicWideMap = site.contacts.find((contact) => contact.kind === 'map' && !contact.branch_id);
  // An uploaded logo is a `storage:` name; its address is the API's fixed public one (§ 50).
  const logoSrc = logoSrcOf(profile?.logo_url);
  const logo = logoSrc ? absoluteUrl(logoSrc) : undefined;

  return compact({
    '@context': 'https://schema.org',
    '@type': 'VeterinaryCare',
    '@id': clinicId(),
    name,
    url: absoluteUrl('/'),
    logo,
    image: logo ?? absoluteUrl(ogImage.path),
    telephone: site.phones[0]?.value,
    email: site.email?.value,
    address: postalAddress(single?.address ?? clinicWideMap?.value),
    geo: geo(single),
    hasMap: isWebLink(site.map?.url) ? site.map.url : undefined,
    openingHoursSpecification: site.hours ? weeklyHoursSchema(site.hours) : undefined,
    specialOpeningHoursSpecification: site.hours ? closuresSchema(site.hours) : undefined,
    contactPoint: site.emergency
      ? [{ '@type': 'ContactPoint', telephone: site.emergency.value, contactType: 'emergency' }]
      : undefined,
    sameAs: profileLinks(site),
    department: site.branches.length > 1 ? site.branches.map((branch) => branchSchema(name, branch, site)) : undefined,
  });
}
