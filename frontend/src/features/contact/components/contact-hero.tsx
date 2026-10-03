import { SiteContainer } from '@/components/layout/site-container';
import { contactPage } from '../content/contact-page';

/** The top of the page: what it is for, in one heading and one paragraph. */
export function ContactHero() {
  return (
    <section aria-labelledby="contact-heading" className="bg-site-forest text-white">
      <SiteContainer className="py-10 lg:py-12">
        <p className="text-sm font-semibold uppercase tracking-wide text-site-orange">{contactPage.eyebrow}</p>
        <h1 id="contact-heading" className="mt-2 text-3xl font-semibold leading-tight sm:text-[38px]">
          {contactPage.heading}
        </h1>
        <p className="mt-4 max-w-[560px] text-base font-light leading-relaxed text-white/90">{contactPage.lead}</p>
      </SiteContainer>
    </section>
  );
}
