import Link from 'next/link';
import { SiteContainer } from '@/components/layout/site-container';
import { SiteCard } from '@/components/site/site-card';
import { contactHref } from '@/features/clinic/lib/contact-links';
import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';
import { hoursRows } from '@/features/clinic/lib/weekdays';
import { location } from '../../content/location';
import type { ContactRow } from '../../types/home.types';
import { ContactTable } from './contact-table';
import { LocationMap } from './location-map';

/**
 * Where the clinic is and how to reach it: details on the left, the map on the right. The details
 * are the clinic's own contact entries and hours, the same ones the footer and contact page show.
 */
export async function LocationSection() {
  const info = await loadSiteInfo();
  const phone = info.phones[0];
  const line = info.socials.find((contact) => contact.kind === 'line');

  const rows: ContactRow[] = [
    info.hours && {
      label: location.labels.hours,
      value: hoursRows(info.hours.days, location.closed).map((row) => `${row.days} ${row.hours}`).join(', '),
    },
    phone && { label: location.labels.phone, value: phone.value, href: contactHref(phone) },
    line && { label: location.labels.line, value: line.value, href: contactHref(line) },
    ...location.extraRows,
  ].filter((row): row is ContactRow => Boolean(row));

  return (
    <section id="contact" aria-labelledby="location-heading" className="scroll-mt-6 py-9">
      <SiteContainer>
        <SiteCard padding="none" className="grid overflow-hidden lg:grid-cols-[623fr_585fr]">
          <div className="px-5 pb-8 pt-7 sm:px-8 sm:pb-10 sm:pt-8">
            <h2 id="location-heading" className="text-2xl font-semibold sm:text-[30px] lg:pl-2">
              {location.name}
            </h2>

            <div className="mt-6">
              <ContactTable rows={rows} />
            </div>

            {info.map && (
              <address className="mt-7 whitespace-pre-line text-[15px] not-italic leading-[19px] text-site-muted">
                {info.map.value}
              </address>
            )}

            <Link href={location.more.href} className="mt-4 inline-block text-[15px] font-semibold text-site-teal hover:underline">
              {location.more.label}
            </Link>
          </div>

          <LocationMap embedUrl={info.map?.embed_url} />
        </SiteCard>
      </SiteContainer>
    </section>
  );
}
