import { SiteContainer } from '@/components/layout/site-container';
import { servicesHero } from '../../content/services-page';

/** The top of the page: what it is for, in one heading and one paragraph. Same band as the contact page. */
export function ServicesHero() {
  return (
    <section aria-labelledby="services-page-heading" className="bg-site-forest text-white">
      <SiteContainer className="py-10 lg:py-12">
        <p className="text-sm font-semibold uppercase tracking-wide text-site-orange">{servicesHero.eyebrow}</p>
        <h1 id="services-page-heading" className="mt-2 text-3xl font-semibold leading-tight sm:text-[38px]">
          {servicesHero.heading}
        </h1>
        <p className="mt-4 max-w-[560px] text-base font-light leading-relaxed text-white/90">{servicesHero.lead}</p>
      </SiteContainer>
    </section>
  );
}
