import { SiteContainer } from '@/components/layout/site-container';
import { policyVersion, privacyHero } from '../content/privacy-policy';

/** The top of the page: what it is, in one heading and one paragraph, and since when it applies. */
export function PrivacyHero() {
  return (
    <section aria-labelledby="privacy-heading" className="bg-site-forest text-white">
      <SiteContainer className="py-10 lg:py-12">
        <p className="text-sm font-semibold uppercase tracking-wide text-site-orange">{privacyHero.eyebrow}</p>
        <h1 id="privacy-heading" className="mt-2 text-3xl font-semibold leading-tight sm:text-[38px]">
          {privacyHero.heading}
        </h1>
        <p className="mt-4 max-w-[600px] text-base font-light leading-relaxed text-white/90">{privacyHero.lead}</p>
        <p className="mt-4 text-sm text-white/75">
          {privacyHero.effective(policyVersion.effectiveDate, policyVersion.version)}
        </p>
      </SiteContainer>
    </section>
  );
}
