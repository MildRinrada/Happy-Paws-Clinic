import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/components/site/section-heading';
import { vaccinePlans } from '../content/vaccine-plans';
import { vaccinePage } from '../content/vaccine-schedule-page';
import { GeneralPlanCard } from './general-plan-card';
import { MedicalDisclaimer } from './medical-disclaimer';

/**
 * The reading part of the page: why vaccines come in rounds, and the general schedule for dogs
 * and cats. It is plain text in the HTML, so search engines and people without JavaScript get it too.
 */
export function VaccineScheduleIntro() {
  const text = vaccinePage.intro;

  return (
    <section aria-labelledby="vaccine-intro-heading" className="bg-site-soft py-12 print:hidden">
      <SiteContainer>
        <article className="max-w-[720px]">
          <SectionHeading id="vaccine-intro-heading">{text.heading}</SectionHeading>
          <div className="mt-5 space-y-4 text-base leading-relaxed text-ink-800">
            {text.paragraphs.map((paragraph) => (
              <p key={paragraph}>{paragraph}</p>
            ))}
          </div>
        </article>

        <div className="mt-8 grid gap-5 lg:grid-cols-2">
          <GeneralPlanCard plan={vaccinePlans.dog} />
          <GeneralPlanCard plan={vaccinePlans.cat} />
        </div>

        <div className="mt-6">
          <MedicalDisclaimer />
        </div>
      </SiteContainer>
    </section>
  );
}
