import { cache } from 'react';
import { listContacts } from '../api/contacts';
import { getOpeningHours } from '../api/getOpeningHours';
import { listBranches } from '../api/listBranches';
import type { Branch, OpeningHours } from '../types/clinic.types';
import type { ClinicContact } from '../types/contact.types';
import { isSocial } from './contact-links';

export interface SiteInfo {
  /** Every entry, in the order the clinic set. */
  contacts: ClinicContact[];
  /** The clinic-wide entries a footer shows, picked out by kind. */
  phones: ClinicContact[];
  emergency?: ClinicContact;
  email?: ClinicContact;
  socials: ClinicContact[];
  map?: ClinicContact;
  hours: OpeningHours | null;
  branches: Branch[];
}

async function quietly<T>(read: () => Promise<T>, fallback: T): Promise<T> {
  try {
    return await read();
  } catch {
    return fallback;
  }
}

/**
 * The clinic's contact entries, opening hours and branches, read once per request and shared by
 * the header, footer and page that all want them. A site whose API is away still renders: each part
 * shows what it has, and a missing part is simply not drawn.
 */
export const loadSiteInfo = cache(async (): Promise<SiteInfo> => {
  const [contacts, hours, branches] = await Promise.all([
    quietly(listContacts, []),
    quietly(getOpeningHours, null),
    quietly(listBranches, []),
  ]);

  // The footer and the home page speak for the whole clinic, so they take clinic-wide entries
  // first and a branch's only when the clinic has none of that kind.
  const wide = [...contacts].sort((a, b) => Number(Boolean(a.branch_id)) - Number(Boolean(b.branch_id)));
  const first = (kind: ClinicContact['kind']) => wide.find((contact) => contact.kind === kind);

  return {
    contacts,
    phones: wide.filter((contact) => contact.kind === 'phone' && !contact.branch_id),
    emergency: first('emergency'),
    email: first('email'),
    socials: wide.filter((contact) => isSocial(contact) && !contact.branch_id),
    map: first('map'),
    hours: hours?.configured ? hours : null,
    branches,
  };
});
