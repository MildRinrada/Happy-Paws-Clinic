import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/components/site/section-heading';
import { CtaLink } from '@/components/ui/cta-link';
import { line } from '../../content/line';
import { LineBenefit } from './line-benefit';

/** The invitation to add the clinic on LINE, and what the owner gets for doing so. */
export function LineSection() {
  return (
    <section aria-labelledby="line-heading" className="bg-line-green pb-14 pt-[58px] text-white">
      <SiteContainer>
        {/* Everything keeps to the left half; the right of the section is left open. */}
        <div className="max-w-[563px]">
          <p className="inline-flex min-h-10 items-center rounded-full bg-white/30 px-6 py-1.5 font-light">{line.pill}</p>

          <div className="mt-7">
            <SectionHeading id="line-heading">
              {line.headingLines.map((headingLine) => (
                <span key={headingLine} className="block">
                  {headingLine}
                </span>
              ))}
            </SectionHeading>
          </div>

          <p className="mt-6 max-w-[450px] text-[15px] font-semibold leading-[19px]">{line.lead}</p>

          <ol className="mt-5 space-y-[17px]">
            {line.benefits.map((benefit, index) => (
              <LineBenefit key={benefit.text} number={index + 1} benefit={benefit} />
            ))}
          </ol>

          <div className="mt-9 flex justify-center">
            <CtaLink href={line.action.href} variant="dark" size="lg" className="w-[212px]">
              {line.action.label}
            </CtaLink>
          </div>
        </div>
      </SiteContainer>
    </section>
  );
}
