import { portalNavLinks } from '../content/portal-navigation';

const recordsPage = portalNavLinks.find((link) => link.icon === 'records')?.href ?? '/portal/records';

/** The records page opened on one pet. */
export function recordsHref(petId: string): string {
  return `${recordsPage}?pet=${encodeURIComponent(petId)}`;
}
