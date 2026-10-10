import { SiteContainer } from '@/components/layout/site-container';
import { ControllerSection } from '@/features/privacy/components/controller-section';
import { PolicyBlock } from '@/features/privacy/components/policy-block';
import { PolicySection } from '@/features/privacy/components/policy-section';
import { PolicyToc } from '@/features/privacy/components/policy-toc';
import { PrivacyHero } from '@/features/privacy/components/privacy-hero';
import { ReviewNotice } from '@/features/privacy/components/review-notice';
import { controllerSection, privacyMeta, privacySections } from '@/features/privacy/content/privacy-policy';
import { loadController } from '@/features/privacy/lib/load-controller';
import { BreadcrumbJsonLd } from '@/features/seo/components/breadcrumb-json-ld';
import { sitePages } from '@/features/seo/content/seo';
import { pageMetadata } from '@/features/seo/lib/page-metadata';

export const metadata = pageMetadata({ ...privacyMeta, path: sitePages.privacy.path });

/**
 * The privacy policy. Words are in `features/privacy/content/privacy-policy.ts`; who the clinic is
 * and how to reach it come from the API, so they follow the clinic's own settings.
 */
export default async function PrivacyPage() {
  const controller = await loadController();
  const toc = [controllerSection, ...privacySections].map((section) => ({
    label: section.heading,
    href: `#${section.id}`,
  }));

  return (
    <main>
      <PrivacyHero />

      <SiteContainer className="grid gap-10 py-10 lg:grid-cols-[260px_minmax(0,1fr)] lg:gap-12">
        <aside className="min-w-0">
          <PolicyToc items={toc} />
        </aside>

        <article className="min-w-0 max-w-[720px] space-y-12">
          <ReviewNotice />
          <ControllerSection info={controller} />
          {privacySections.map((section) => (
            <PolicySection key={section.id} id={section.id} heading={section.heading}>
              {section.blocks.map((block, index) => (
                <PolicyBlock key={index} block={block} />
              ))}
            </PolicySection>
          ))}
        </article>
      </SiteContainer>

      <BreadcrumbJsonLd trail={[sitePages.privacy]} />
    </main>
  );
}
