import { ImageSlot } from '@/components/shared/image-slot';
import { careHeading, careImage, carePromises } from '../../content/care';
import { SectionContainer } from '../shared/section-container';
import { SectionHeading } from '../shared/section-heading';
import { CarePromiseCard } from './care-promise-card';

/** How the clinic treats its patients, as four promises beside a picture. */
export function CareSection() {
  return (
    <section id="about" aria-labelledby="care-heading" className="scroll-mt-6 bg-site-forest py-12 text-white">
      <SectionContainer className="grid items-center gap-9 lg:grid-cols-[584fr_725fr] xl:gap-14">
        <ImageSlot
          src={careImage.src}
          alt={careImage.alt}
          label="รูปประกอบ (สัตวแพทย์กับลูกสุนัข)"
          tone="onDark"
          sizes="(min-width: 1024px) 40vw, 100vw"
          className="aspect-[584/473] w-full overflow-hidden rounded-3xl"
        />

        <div>
          <SectionHeading id="care-heading">{careHeading}</SectionHeading>

          <ol className="mt-5 space-y-3.5">
            {carePromises.map((promise, index) => (
              <CarePromiseCard key={promise.title} number={index + 1} promise={promise} />
            ))}
          </ol>
        </div>
      </SectionContainer>
    </section>
  );
}
