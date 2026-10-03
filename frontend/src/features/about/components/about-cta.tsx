import { SiteContainer } from '@/components/layout/site-container';
import { CtaLink } from '@/components/ui/cta-link';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
import { aboutCta } from '../content/about-page';

/** The close of the page: meet the team, or book. Same orange band as the home page's close. */
export function AboutCta() {
  return (
    <section aria-labelledby="about-cta-heading" className="bg-site-orange pb-14 pt-9 text-white">
      <SiteContainer className="flex flex-wrap items-center justify-between gap-x-10 gap-y-7">
        <div>
          <SectionHeading id="about-cta-heading">{aboutCta.heading}</SectionHeading>
          <p className="mt-4 text-[15px] font-semibold">{aboutCta.lead}</p>
        </div>

        <div className="flex flex-wrap gap-4">
          <CtaLink href={aboutCta.primaryAction.href} variant="violet" size="lg">
            {aboutCta.primaryAction.label}
          </CtaLink>
          <CtaLink href={aboutCta.secondaryAction.href} variant="glass" size="lg">
            {aboutCta.secondaryAction.label}
          </CtaLink>
        </div>
      </SiteContainer>
    </section>
  );
}
