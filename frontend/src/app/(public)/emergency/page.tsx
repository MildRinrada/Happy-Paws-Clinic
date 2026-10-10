import { SiteContainer } from '@/components/layout/site-container';
import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';
import { CareStepsSection } from '@/features/emergency/components/care-steps-section';
import { ClinicalNote } from '@/features/emergency/components/clinical-note';
import { EmergencyHero } from '@/features/emergency/components/emergency-hero';
import { EmergencyMapSection } from '@/features/emergency/components/emergency-map-section';
import { OpenStatusSection } from '@/features/emergency/components/open-status-section';
import { WarningSignsSection } from '@/features/emergency/components/warning-signs-section';
import { emergencyPage } from '@/features/emergency/content/emergency-page';
import { callNumbers } from '@/features/emergency/lib/call-numbers';
import { mapEntries } from '@/features/emergency/lib/map-entries';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { ClinicJsonLd } from '@/features/seo/components/clinic-json-ld';
import { EmergencyJsonLd } from '@/features/seo/components/emergency-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';

export const metadata = pageMetadata({ ...emergencyPage.meta, path: sitePages.emergency.path });

// "Open now" is worked out for the moment of the visit, so the page is drawn per request and never
// frozen at build time.
export const dynamic = 'force-dynamic';

/**
 * What to do when a pet cannot wait: the numbers to ring, whether the clinic is open right now,
 * the signs that need a vet at once, what to do and not do on the way, and the way there.
 *
 * The numbers, hours, closures and maps are the clinic's own entries (`GET /clinic/contacts`,
 * `GET /clinic/hours`, `GET /clinic/branches`), the same read the footer and contact page make. The
 * medical advice is `features/emergency/content/emergency-care.ts`, which a clinic vet signs off.
 */
export default async function EmergencyPage() {
  const info = await loadSiteInfo();
  const numbers = callNumbers(info);

  return (
    <main>
      <EmergencyHero numbers={numbers} />

      <SiteContainer className="space-y-12 py-10 lg:py-12">
        <OpenStatusSection hours={info.hours} branches={info.branches} now={new Date()} />
        <WarningSignsSection />
        <CareStepsSection />
        <ClinicalNote />
        <EmergencyMapSection maps={mapEntries(info)} />
      </SiteContainer>

      <ClinicJsonLd />
      <EmergencyJsonLd telephone={numbers[0]?.value} />
      <BreadcrumbJsonLd trail={[sitePages.emergency]} />
    </main>
  );
}
