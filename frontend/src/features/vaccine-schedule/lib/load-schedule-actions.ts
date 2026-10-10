import { contactHref } from '@/features/clinic/lib/contact-links';
import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';
import { listServices } from '@/features/service-catalog/api/listServices';
import { qrCodeDataUrl } from '@/features/staff/lib/qr-code';
import type { LineLink, ScheduleActions } from '../types/vaccine-schedule.types';
import { vaccineBookLinks } from './book-links';

async function quietly<T>(read: () => Promise<T>, fallback: T): Promise<T> {
  try {
    return await read();
  } catch {
    return fallback;
  }
}

async function loadLine(): Promise<LineLink | undefined> {
  const info = await loadSiteInfo();
  // The clinic-wide account first, as the footer and home page do; a branch's only if there is no other.
  const line = info.socials.find((contact) => contact.kind === 'line') ?? info.contacts.find((contact) => contact.kind === 'line');
  const href = line && contactHref(line);

  if (!line || !href || !/^https?:\/\//i.test(href)) {
    return undefined;
  }

  return { href, handle: line.value, qrDataUrl: await quietly(() => qrCodeDataUrl(href), undefined) };
}

/**
 * What the page's calls to action point at, read on the server: the clinic's LINE account from its
 * contact entries (`GET /clinic/contacts`) and a vaccination service from the public menu
 * (`GET /services`). Either read failing leaves its action out or plain, never the page broken.
 */
export async function loadScheduleActions(): Promise<ScheduleActions> {
  const [line, services] = await Promise.all([quietly(loadLine, undefined), quietly(() => listServices(), [])]);

  return { line, book: vaccineBookLinks(services) };
}
