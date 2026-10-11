import { loadSiteInfo } from '@/features/clinic/lib/load-site-info';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { ServiceCatalogJsonLd } from '@/features/seo/components/service-catalog-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';
import { ServiceMenu } from '@/features/service-catalog/components/public/service-menu';
import { ServicesBanner } from '@/features/service-catalog/components/public/services-banner';
import { ServicesCta } from '@/features/service-catalog/components/public/services-cta';
import { servicesMeta } from '@/features/service-catalog/content/services-page';
import { askChannels } from '@/features/service-catalog/lib/ask-channels';
import { loadServiceMenu } from '@/features/service-catalog/lib/load-service-menu';
import { loadPageBanner } from '@/features/site-banners/lib/load-page-banner';

// Never prerendered at build time: a build without the API would freeze a page that says the menu
// could not be loaded.
export const dynamic = 'force-dynamic';

export const metadata = pageMetadata({ ...servicesMeta, path: sitePages.services.path });

/**
 * Services and prices, read from the public menu (`GET /services`): only what the clinic has
 * opened for online booking, under every category (`GET /services/categories`), a category with
 * none saying to call. Each service's button opens booking with that service chosen. The phone
 * and LINE come from the clinic's contact entries (`GET /clinic/contacts`), the banner from
 * `loadPageBanner('services')`. Rendered on each request, so a price change shows at once.
 */
export default async function ServicesPage() {
  const [banner, menu, info] = await Promise.all([loadPageBanner('services'), loadServiceMenu(), loadSiteInfo()]);
  const channels = askChannels(info);

  return (
    <main>
      <ServicesBanner banner={banner} />
      <ServiceMenu menu={menu} phone={channels.phone} />
      <ServicesCta channels={channels} />

      <ServiceCatalogJsonLd />
      <BreadcrumbJsonLd trail={[sitePages.services]} />
    </main>
  );
}
