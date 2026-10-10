import type { ReactNode } from 'react';
import { SiteContainer } from '@/components/layout/site-container';
import { controllerSection, policyUnavailable } from '../content/privacy-policy';
import type { ControllerInfo } from '../lib/load-controller';
import type { PublicPrivacyPolicy } from '../types/privacy-policy.types';
import { ControllerSection } from './controller-section';
import { PolicyBody } from './policy-body';
import { PolicyHistory } from './policy-history';
import { PolicyNotPublished } from './policy-not-published';
import { PolicyParagraph } from './policy-paragraph';
import { PolicySection } from './policy-section';
import { PolicyToc } from './policy-toc';
import { PrivacyHero } from './privacy-hero';
import { ReviewNotice } from './review-notice';

interface PrivacyDocumentProps {
  controller: ControllerInfo | null;
  /** The version to show; `null` when the API could not be read. */
  policy: PublicPrivacyPolicy | null;
  /** Above everything else in the text column, such as the "older version" banner. */
  notice?: ReactNode;
}

/**
 * The whole notice page, for the current version and for an older one alike. The clinic's details
 * always come first, from its own settings. Then the published text, or, when the clinic has not
 * published one, a plain statement that it has not.
 */
export function PrivacyDocument({ controller, policy, notice }: PrivacyDocumentProps) {
  const sections = policy?.published ? (policy.sections ?? []) : [];
  const history = policy?.history ?? [];
  const toc = [
    { label: controllerSection.heading, href: `#${controllerSection.id}` },
    ...sections.map((section) => ({ label: section.heading, href: `#${section.anchor}` })),
  ];

  return (
    <main>
      <PrivacyHero label={policy?.published ? policy.label : undefined} effectiveDate={policy?.effective_date} />

      <SiteContainer className="grid gap-10 py-10 lg:grid-cols-[260px_minmax(0,1fr)] lg:gap-12">
        <aside className="min-w-0">
          <PolicyToc items={toc} />
        </aside>

        <article className="min-w-0 max-w-[720px] space-y-12">
          {notice}
          {policy === null && <PolicyParagraph>{policyUnavailable}</PolicyParagraph>}
          {policy && !policy.published && <PolicyNotPublished />}
          {policy?.published && !policy.legal_review_done && <ReviewNotice />}

          <ControllerSection info={controller} />

          {sections.map((section) => (
            <PolicySection key={section.anchor} id={section.anchor} heading={section.heading}>
              <PolicyBody body={section.body} />
            </PolicySection>
          ))}

          {history.length > 1 && <PolicyHistory entries={history} shown={policy?.label} />}
        </article>
      </SiteContainer>
    </main>
  );
}
