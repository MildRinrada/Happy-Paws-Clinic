import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
import { carePromises } from '@/features/home/content/care';
import { aboutValues } from '../content/about-page';
import { ValueCard } from './value-card';

/**
 * The clinic's promises. The same four as on the home page, read from the home page's content
 * file, so changing one wording changes both pages.
 */
export function ValuesSection() {
  return (
    <section aria-labelledby="values-heading" className="bg-site-soft py-12">
      <SiteContainer>
        <SectionHeading id="values-heading">{aboutValues.heading}</SectionHeading>
        <p className="mt-2 text-[15px] text-site-muted">{aboutValues.lead}</p>

        <ol className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {carePromises.map((value, index) => (
            <ValueCard key={value.title} number={index + 1} value={value} />
          ))}
        </ol>
      </SiteContainer>
    </section>
  );
}
