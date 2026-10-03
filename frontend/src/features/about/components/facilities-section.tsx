import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
import { aboutFacilities } from '../content/about-page';
import { FacilityCard } from './facility-card';

/** What the clinic has under one roof. */
export function FacilitiesSection() {
  return (
    <section aria-labelledby="facilities-heading" className="py-12">
      <SiteContainer>
        <SectionHeading id="facilities-heading">{aboutFacilities.heading}</SectionHeading>
        <p className="mt-2 text-[15px] text-site-muted">{aboutFacilities.lead}</p>

        <ul className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {aboutFacilities.items.map((item) => (
            <FacilityCard key={item.title} title={item.title} detail={item.detail} />
          ))}
        </ul>
      </SiteContainer>
    </section>
  );
}
