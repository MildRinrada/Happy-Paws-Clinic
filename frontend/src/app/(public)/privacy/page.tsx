import { getCurrentPrivacyPolicy } from '@/features/privacy/api/getPrivacyPolicy';
import { PrivacyDocument } from '@/features/privacy/components/privacy-document';
import { privacyMeta } from '@/features/privacy/content/privacy-policy';
import { loadController } from '@/features/privacy/lib/load-controller';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';

export const metadata = pageMetadata({ ...privacyMeta, path: sitePages.privacy.path });

/**
 * The privacy notice the clinic has published (docs/contract-extensions.md § 35), read from the API
 * and kept for a minute; publishing in the console refreshes it at once. Before the clinic
 * publishes one, the page says so and shows how to reach the clinic, never the shipped template.
 * Who the clinic is comes from its own settings.
 */
export default async function PrivacyPage() {
  const [controller, policy] = await Promise.all([loadController(), getCurrentPrivacyPolicy().catch(() => null)]);

  return (
    <>
      <PrivacyDocument controller={controller} policy={policy} />
      <BreadcrumbJsonLd trail={[sitePages.privacy]} />
    </>
  );
}
