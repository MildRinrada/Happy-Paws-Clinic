import type { Branch } from '@/features/clinic/types/clinic.types';
import type { ClinicContact } from '@/features/clinic/types/contact.types';

export interface ContactGroup {
  key: string;
  /** Absent for the clinic-wide group when there is nothing else to tell it apart from. */
  title?: string;
  /** Phones, email, chat and the rest, drawn as cards. */
  channels: ClinicContact[];
  maps: ClinicContact[];
}

/**
 * The entries split for the page: the clinic-wide ones first, then one group per branch that has
 * any, in the order the branches are listed. Emergency entries are left out; the page shows the
 * first of them on its own, above everything.
 */
export function groupContacts(contacts: ClinicContact[], branches: Branch[], clinicWideTitle: string): ContactGroup[] {
  const shown = contacts.filter((contact) => contact.kind !== 'emergency');
  const split = (items: ClinicContact[]) => ({
    channels: items.filter((contact) => contact.kind !== 'map'),
    maps: items.filter((contact) => contact.kind === 'map'),
  });

  const branchGroups = branches
    .map((branch) => ({ key: branch.id, title: branch.name, ...split(shown.filter((contact) => contact.branch_id === branch.id)) }))
    .filter((group) => group.channels.length + group.maps.length > 0);

  const wide = split(shown.filter((contact) => !contact.branch_id));
  const groups: ContactGroup[] = [];

  if (wide.channels.length + wide.maps.length > 0) {
    groups.push({ key: 'clinic', title: branchGroups.length > 0 ? clinicWideTitle : undefined, ...wide });
  }

  return [...groups, ...branchGroups];
}
