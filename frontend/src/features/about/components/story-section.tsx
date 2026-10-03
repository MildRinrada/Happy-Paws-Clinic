import { SiteContainer } from '@/components/layout/site-container';
import { ImageSlot } from '@/components/shared/image-slot';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
import { aboutStory } from '../content/about-page';
import { MilestoneItem } from './milestone-item';

/** How the clinic began and grew: two paragraphs, a photo, and the years that mattered. */
export function StorySection() {
  return (
    <section aria-labelledby="story-heading" className="py-12">
      <SiteContainer className="grid gap-9 lg:grid-cols-[380px_1fr] lg:gap-12">
        <ImageSlot
          src={aboutStory.image.src}
          alt={aboutStory.image.alt}
          label="รูปประกอบ (แมวในกระเป๋า)"
          sizes="(min-width: 1024px) 380px, 100vw"
          className="aspect-[4/3] w-full overflow-hidden rounded-3xl lg:aspect-auto lg:h-full lg:min-h-[360px]"
        />

        <div>
          <SectionHeading id="story-heading">{aboutStory.heading}</SectionHeading>
          <div className="mt-4 space-y-3 text-[15px] leading-relaxed text-ink-700">
            {aboutStory.paragraphs.map((paragraph) => (
              <p key={paragraph}>{paragraph}</p>
            ))}
          </div>

          <h3 className="mt-8 text-sm font-semibold text-site-muted">{aboutStory.milestonesLabel}</h3>
          <ol className="relative mt-3 space-y-4 before:absolute before:bottom-2 before:left-[5px] before:top-2 before:w-0.5 before:bg-site-teal/25">
            {aboutStory.milestones.map((milestone) => (
              <MilestoneItem key={milestone.year} year={milestone.year} text={milestone.text} />
            ))}
          </ol>
        </div>
      </SiteContainer>
    </section>
  );
}
