import { contactHref } from '@/features/clinic/lib/contact-links';
import type { SiteInfo } from '@/features/clinic/lib/load-site-info';
import type { AskChannels } from '../types/ask-channels.types';

/**
 * The clinic's main phone and LINE account, from its contact entries (`GET /clinic/contacts`, read
 * once per request by `loadSiteInfo`, as the header and footer do). Clinic-wide entries come
 * first, a branch's only when the clinic has none of that kind. A LINE entry without a web link
 * is left out, since there is nothing to open.
 */
export function askChannels(info: SiteInfo): AskChannels {
  const phone = info.phones[0] ?? info.contacts.find((contact) => contact.kind === 'phone');
  const line = info.socials.find((contact) => contact.kind === 'line') ?? info.contacts.find((contact) => contact.kind === 'line');
  const phoneHref = phone && contactHref(phone);
  const lineHref = line && contactHref(line);

  return {
    phone: phone && phoneHref ? { number: phone.value, href: phoneHref } : undefined,
    line: lineHref && /^https?:\/\//i.test(lineHref) ? { href: lineHref } : undefined,
  };
}
