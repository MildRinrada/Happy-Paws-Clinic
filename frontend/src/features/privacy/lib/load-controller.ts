import { getClinicProfile } from '@/features/clinic/api/getClinicProfile';
import { contactHref } from '@/features/clinic/lib/contact-links';
import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';

export interface ControllerContact {
  label: string;
  value: string;
  href?: string;
}

/** Who the data controller is, as the clinic has set itself up. Absent fields were never set. */
export interface ControllerInfo {
  clinicName?: string;
  operatorName?: string;
  licenseNo?: string;
  address?: string;
  contacts: ControllerContact[];
}

/**
 * The clinic's name, operator, licence, address and ways to reach it, for the policy's first
 * section. Read from `GET /clinic` and `GET /clinic/contacts`, the same settings the footer and
 * the contact page show, so the policy names whoever the clinic says it is.
 *
 * Answers `null` when neither can be read: the page then points to the contact page rather than
 * printing an empty box.
 */
export async function loadController(): Promise<ControllerInfo | null> {
  const [profile, site] = await Promise.all([getClinicProfile().catch(() => null), loadSiteInfo()]);

  const contacts: ControllerContact[] = [...site.phones, ...(site.email ? [site.email] : [])].map((contact) => ({
    label: contact.label,
    value: contact.value,
    href: contactHref(contact),
  }));

  // The footer reads the address from the "map" contact entry; a branch's own address is the fallback.
  const address = site.map?.value || site.branches.find((branch) => branch.address)?.address;

  if (!profile && contacts.length === 0 && !address) {
    return null;
  }

  return {
    clinicName: profile?.name,
    operatorName: profile?.operator_name,
    licenseNo: profile?.license_no,
    address,
    contacts,
  };
}
