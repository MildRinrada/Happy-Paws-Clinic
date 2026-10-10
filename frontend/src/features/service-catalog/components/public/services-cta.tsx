import { SiteContainer } from '@/components/layout/site-container';
import { CtaLink } from '@/components/ui/cta-link';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
import { servicesCta } from '../../content/services-page';

/** The close of the page: book, or ask the clinic. Same orange band as the home and about pages. */
export function ServicesCta() {
  return (
    <section aria-labelledby="services-cta-heading" className="bg-site-orange pb-14 pt-9 text-white">
      <SiteContainer className="flex flex-wrap items-center justify-between gap-x-10 gap-y-7">
        <div>
          <SectionHeading id="services-cta-heading">{servicesCta.heading}</SectionHeading>
          <p className="mt-4 text-[15px] font-semibold">{servicesCta.lead}</p>
        </div>

        <div className="flex flex-wrap gap-4">
          <CtaLink href={servicesCta.primaryAction.href} variant="violet" size="lg">
            {servicesCta.primaryAction.label}
          </CtaLink>
          <CtaLink href={servicesCta.secondaryAction.href} variant="glass" size="lg">
            {servicesCta.secondaryAction.label}
          </CtaLink>
        </div>
      </SiteContainer>
    </section>
  );
}
