import { hero } from '../../content/hero';
import { SectionContainer } from '../shared/section-container';
import { HeroActions } from './hero-actions';
import { HeroHeading } from './hero-heading';
import { HeroImage } from './hero-image';
import { SocialProof } from './social-proof';

/** The opening of the page: what the clinic is, the two things to do next, and the banner. */
export function HeroSection() {
  return (
    <section className="relative overflow-hidden bg-site-teal text-white">
      <SectionContainer>
        {/* The text keeps to the left so the banner has the right of the section to itself. */}
        <div className="relative z-10 py-12 lg:w-[54%] lg:py-16 xl:w-[58%] xl:pb-[72px] xl:pt-[76px]">
          <HeroHeading />

          <p className="mt-7 max-w-[520px] text-base font-light leading-relaxed sm:text-lg sm:leading-[25px] xl:mt-9">
            {hero.lead}
          </p>

          <div className="mt-8 xl:mt-9">
            <HeroActions />
          </div>

          <div className="mt-8">
            <SocialProof />
          </div>
        </div>
      </SectionContainer>

      <HeroImage />
    </section>
  );
}
