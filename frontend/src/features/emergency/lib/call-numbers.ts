import { contactHref } from '@/features/clinic/lib/contact-links';
import type { SiteInfo } from '@/features/clinic/lib/load-site-info';
import type { ClinicContact } from '@/features/clinic/types/contact.types';
import type { CallNumber } from '../types/emergency.types';

/**
 * The numbers at the top of the page: every emergency entry the clinic has set, the clinic-wide
 * ones first and then each branch's in the order the branches are listed. With no emergency entry
 * at all, the clinic's main phone (the first clinic-wide one, or any phone) stands in.
 */
export function callNumbers(info: SiteInfo): CallNumber[] {
  const multiBranch = info.branches.length > 1;
  const rank = (contact: ClinicContact) =>
    contact.branch_id ? 1 + Math.max(0, info.branches.findIndex((branch) => branch.id === contact.branch_id)) : 0;

  // The API lists entries in the clinic's own order, and a stable sort keeps it within each group.
  const emergency = info.contacts.filter((contact) => contact.kind === 'emergency').sort((a, b) => rank(a) - rank(b));
  const mainPhone = info.phones[0] ?? info.contacts.find((contact) => contact.kind === 'phone');
  const chosen = emergency.length > 0 ? emergency : mainPhone ? [mainPhone] : [];

  return chosen.flatMap((contact) => {
    const href = contactHref(contact);
    const branch = multiBranch ? info.branches.find((item) => item.id === contact.branch_id) : undefined;

    return href
      ? [{ id: contact.id, label: contact.label, value: contact.value, href, branchName: branch?.name, fallback: contact.kind !== 'emergency' }]
      : [];
  });
}
