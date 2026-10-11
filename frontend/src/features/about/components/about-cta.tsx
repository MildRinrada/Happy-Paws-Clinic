import { CtaBand } from '@/components/site/cta-band';
import { CtaLink } from '@/components/ui/cta-link';
import { aboutCta } from '../content/about-page';

/** The close of the page: meet the team, or book. Same orange band as the home page's close. */
export function AboutCta() {
  return (
    <CtaBand
      heading={aboutCta.heading}
      lead={aboutCta.lead}
      actions={
        <>
          <CtaLink href={aboutCta.primaryAction.href} variant="violet" size="lg">
            {aboutCta.primaryAction.label}
          </CtaLink>
          <CtaLink href={aboutCta.secondaryAction.href} variant="glass" size="lg">
            {aboutCta.secondaryAction.label}
          </CtaLink>
        </>
      }
    />
  );
}
