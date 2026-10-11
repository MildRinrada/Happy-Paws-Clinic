import { PageBanner } from '@/components/site/page-banner';
import { privacyHero } from '../content/privacy-policy';
import { formatPolicyDate } from '../lib/policy-date';

interface PrivacyHeroProps {
  /** The version shown, when one is published. */
  label?: string;
  effectiveDate?: string;
}

/** The top of the page: what it is, in one heading and one paragraph, and since when it applies. */
export function PrivacyHero({ label, effectiveDate }: PrivacyHeroProps) {
  return (
    <PageBanner eyebrow={privacyHero.eyebrow} heading={privacyHero.heading} lead={privacyHero.lead} leadWidth="roomy">
      {label && effectiveDate && (
        <p className="mt-4 text-sm text-white/75">{privacyHero.effective(formatPolicyDate(effectiveDate), label)}</p>
      )}
    </PageBanner>
  );
}
