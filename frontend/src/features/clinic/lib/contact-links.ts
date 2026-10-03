import type { ClinicContact, ContactKind } from '../types/contact.types';

/**
 * Where an entry takes you. A phone number becomes a call link and an email a mail link, built
 * from the value; everything else uses the link stored with it, when there is one.
 */
export function contactHref(contact: Pick<ClinicContact, 'kind' | 'value' | 'url'>): string | undefined {
  switch (contact.kind) {
    case 'phone':
    case 'emergency': {
      const digits = contact.value.replace(/[^\d+]/g, '');
      return digits ? `tel:${digits}` : undefined;
    }
    case 'email':
      return `mailto:${contact.value.trim()}`;
    default:
      return contact.url;
  }
}

/** The kinds drawn as an app icon in a row of chat channels. */
export const SOCIAL_KINDS: ContactKind[] = ['line', 'facebook', 'instagram'];

export const isSocial = (contact: ClinicContact) => SOCIAL_KINDS.includes(contact.kind);
