import { featuredServices, moreServices, servicesHeading } from '../../content/services';
import { SectionContainer } from '../shared/section-container';
import { SectionHeading } from '../shared/section-heading';
import { FeaturedServiceCard } from './featured-service-card';
import { MoreServiceCard } from './more-service-card';

const grid = 'grid gap-5 md:grid-cols-2 lg:grid-cols-3 xl:gap-x-8';

/** What the clinic offers: three services in full, three more in brief. */
export function ServicesSection() {
  return (
    <section id="services" aria-labelledby="services-heading" className="scroll-mt-6 pb-10 pt-12">
      <SectionContainer>
        <SectionHeading id="services-heading">{servicesHeading}</SectionHeading>

        <ul className={`mt-7 ${grid}`}>
          {featuredServices.map((service) => (
            <FeaturedServiceCard key={service.id} service={service} />
          ))}
        </ul>

        <ul className={`mt-5 xl:mt-7 ${grid}`}>
          {moreServices.map((service) => (
            <MoreServiceCard key={service.id} service={service} />
          ))}
        </ul>
      </SectionContainer>
    </section>
  );
}
