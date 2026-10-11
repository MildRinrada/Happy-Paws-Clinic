import { loadServiceMenu } from '@/features/service-catalog/lib/load-service-menu';
import { seoSite } from '../content/seo';
import { loadClinicProfile } from '../lib/load-clinic-profile';
import { serviceCatalogSchema } from '../lib/service-catalog-schema';
import { JsonLd } from './json-ld';

/** The services and their prices for search engines. Nothing when the menu could not be read or has no services. */
export async function ServiceCatalogJsonLd() {
  const [menu, profile] = await Promise.all([loadServiceMenu(), loadClinicProfile()]);

  if (!menu || menu.categories.every((category) => category.services.length === 0)) {
    return null;
  }

  return <JsonLd data={serviceCatalogSchema(menu, profile?.name || seoSite.name)} />;
}
