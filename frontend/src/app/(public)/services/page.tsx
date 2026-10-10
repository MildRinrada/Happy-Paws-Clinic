import { SiteContainer } from '@/components/layout/site-container';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { ServiceCatalogJsonLd } from '@/features/seo/components/service-catalog-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';
import { ServiceMenu } from '@/features/service-catalog/components/public/service-menu';
import { ServicesCta } from '@/features/service-catalog/components/public/services-cta';
import { ServicesHero } from '@/features/service-catalog/components/public/services-hero';
import { ServicesNote } from '@/features/service-catalog/components/public/services-note';
import { servicesMeta } from '@/features/service-catalog/content/services-page';
import { loadServiceMenu } from '@/features/service-catalog/lib/load-service-menu';

// Never prerendered at build time: a build without the API would freeze a page that says the menu
// could not be loaded.
export const dynamic = 'force-dynamic';

export const metadata = pageMetadata({ ...servicesMeta, path: sitePages.services.path });

/**
 * Services and prices, read from the public menu (`GET /services`): only what the clinic has
 * opened for online booking. Each service's button opens booking with that service chosen.
 * Rendered on each request, so a price change shows at once.
 */
export default async function ServicesPage() {
  const menu = await loadServiceMenu();

  return (
    <main>
      <ServicesHero />

      <SiteContainer className="space-y-12 py-10">
        <ServiceMenu menu={menu} />
        <ServicesNote />
      </SiteContainer>

      <ServicesCta />

      <ServiceCatalogJsonLd />
      <BreadcrumbJsonLd trail={[sitePages.services]} />
    </main>
  );
}
