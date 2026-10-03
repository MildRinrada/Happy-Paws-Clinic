import { SiteContainer } from '@/components/layout/site-container';
import { ImageSlot } from '@/components/shared/image-slot';
import { aboutHero } from '../content/about-page';

/** The top of the page: who the clinic is, in a heading and one paragraph, beside a photo. */
export function AboutHero() {
  return (
    <section aria-labelledby="about-heading" className="bg-site-forest text-white">
      <SiteContainer className="grid items-center gap-8 py-12 lg:grid-cols-[1fr_440px] lg:gap-12 lg:py-16">
        <div>
          <p className="text-sm font-semibold uppercase tracking-wide text-site-orange">{aboutHero.eyebrow}</p>
          <h1 id="about-heading" className="mt-3 text-3xl font-semibold leading-tight sm:text-[40px] sm:leading-[1.2]">
            {aboutHero.heading.map((line) => (
              <span key={line} className="block">
                {line}
              </span>
            ))}
          </h1>
          <p className="mt-6 max-w-[520px] text-base font-light leading-relaxed text-white/90 sm:text-lg">
            {aboutHero.lead}
          </p>
        </div>

        <ImageSlot
          src={aboutHero.image.src}
          alt={aboutHero.image.alt}
          label="รูปประกอบ (สัตวแพทย์ตรวจสุนัข)"
          tone="onDark"
          priority
          sizes="(min-width: 1024px) 440px, 100vw"
          className="aspect-[4/3] w-full overflow-hidden rounded-3xl"
        />
      </SiteContainer>
    </section>
  );
}
