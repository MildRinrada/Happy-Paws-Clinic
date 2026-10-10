import { Suspense } from 'react';
import { SiteContainer } from '@/components/layout/site-container';
import { CtaLink } from '@/components/ui/cta-link';
import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';
import { ContactGroupSection } from '@/features/contact/components/contact-group-section';
import { ContactHero } from '@/features/contact/components/contact-hero';
import { ContactHours } from '@/features/contact/components/contact-hours';
import { EmergencyCard } from '@/features/contact/components/emergency-card';
import { contactPage } from '@/features/contact/content/contact-page';
import { groupContacts } from '@/features/contact/lib/group-contacts';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { ClinicJsonLd } from '@/features/seo/components/clinic-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';
import { WalkInWait } from '@/features/wait-estimate/components/walk-in-wait';

export const metadata = pageMetadata({ ...contactPage.meta, path: sitePages.contact.path });

/**
 * How to reach the clinic. Every number, account and map here is one of the clinic's contact
 * entries, edited in the staff console; the footer and the home page read the same list. Beside
 * the hours, the walk-in wait now (`features/wait-estimate`), hidden when it cannot be read.
 */
export default async function ContactPage() {
  const info = await loadSiteInfo();
  const groups = groupContacts(info.contacts, info.branches, contactPage.clinicWide);

  return (
    <main>
      <ContactHero />

      <SiteContainer className="space-y-10 py-10">
        {info.emergency && <EmergencyCard contact={info.emergency} />}

        <div className="grid gap-10 lg:grid-cols-[1fr_340px]">
          <section aria-labelledby="channels-heading" className="min-w-0 space-y-6">
            <h2 id="channels-heading" className="text-xl font-semibold">
              {contactPage.channelsHeading}
            </h2>
            {groups.length === 0 ? (
              <p className="text-[15px] text-site-muted">{contactPage.empty}</p>
            ) : (
              groups.map((group) => <ContactGroupSection key={group.key} group={group} />)
            )}
          </section>

          <aside className="space-y-8">
            <Suspense fallback={null}>
              <WalkInWait />
            </Suspense>
            <ContactHours hours={info.hours} />
          </aside>
        </div>
      </SiteContainer>

      <section aria-labelledby="contact-cta-heading" className="bg-site-orange py-9 text-white">
        <SiteContainer className="flex flex-wrap items-center justify-between gap-6">
          <div>
            <h2 id="contact-cta-heading" className="text-2xl font-semibold">
              {contactPage.cta.heading}
            </h2>
            <p className="mt-2 text-[15px] font-semibold">{contactPage.cta.lead}</p>
          </div>
          <CtaLink href={contactPage.cta.book.href} variant="violet" size="lg">
            {contactPage.cta.book.label}
          </CtaLink>
        </SiteContainer>
      </section>

      <ClinicJsonLd />
      <BreadcrumbJsonLd trail={[sitePages.contact]} />
    </main>
  );
}
