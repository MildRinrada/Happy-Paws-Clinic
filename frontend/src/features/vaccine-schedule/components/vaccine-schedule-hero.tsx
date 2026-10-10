import { SiteContainer } from '@/components/layout/site-container';
import { vaccinePage } from '../content/vaccine-schedule-page';

/** The top of the page: what the tool does, in one heading and one paragraph. */
export function VaccineScheduleHero() {
  const text = vaccinePage.hero;

  return (
    <section aria-labelledby="vaccine-heading" className="bg-site-forest text-white print:hidden">
      <SiteContainer className="py-10 lg:py-12">
        <p className="text-sm font-semibold uppercase tracking-wide text-site-orange">{text.eyebrow}</p>
        <h1 id="vaccine-heading" className="mt-2 text-3xl font-semibold leading-tight sm:text-[38px]">
          {text.heading}
        </h1>
        <p className="mt-4 max-w-[600px] text-base font-light leading-relaxed text-white/90">{text.lead}</p>
      </SiteContainer>
    </section>
  );
}
