import { AboutCta } from '@/features/about/components/about-cta';
import { AboutHero } from '@/features/about/components/about-hero';
import { FacilitiesSection } from '@/features/about/components/facilities-section';
import { LicenceSection } from '@/features/about/components/licence-section';
import { StorySection } from '@/features/about/components/story-section';
import { ValuesSection } from '@/features/about/components/values-section';
import { VisitSection } from '@/features/about/components/visit-section';
import { aboutMeta } from '@/features/about/content/about-page';
import { loadVisitInfo } from '@/features/about/lib/load-visit-info';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';

export const metadata = pageMetadata({ ...aboutMeta, path: sitePages.about.path });

/**
 * About the clinic, top to bottom. Words are in `features/about/content/about-page.ts`; the
 * branches and opening hours come from the API, so they follow the clinic's own settings.
 */
export default async function AboutPage() {
  const visit = await loadVisitInfo();

  return (
    <main>
      <AboutHero />
      <StorySection />
      <ValuesSection />
      <FacilitiesSection />
      <VisitSection info={visit} />
      <LicenceSection />
      <AboutCta />
      <BreadcrumbJsonLd trail={[sitePages.about]} />
    </main>
  );
}
