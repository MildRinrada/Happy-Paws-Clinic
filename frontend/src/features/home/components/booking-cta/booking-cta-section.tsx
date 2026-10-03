import { CtaLink } from '@/components/ui/cta-link';
import { bookingCta } from '../../content/booking-cta';
import { SectionContainer } from '../shared/section-container';
import { SectionHeading } from '../shared/section-heading';

/** The last thing on the page: an invitation to book, with sign-in beside it. */
export function BookingCtaSection() {
  return (
    <section aria-labelledby="booking-cta-heading" className="bg-site-orange pb-14 pt-9 text-white">
      <SectionContainer className="flex flex-wrap items-center justify-between gap-x-10 gap-y-7">
        <div>
          <SectionHeading id="booking-cta-heading">{bookingCta.heading}</SectionHeading>
          <p className="mt-4 text-[15px] font-semibold">{bookingCta.lead}</p>
        </div>

        <div className="flex flex-wrap gap-4 lg:gap-[22px]">
          <CtaLink href={bookingCta.primaryAction.href} variant="violet" size="lg">
            {bookingCta.primaryAction.label}
          </CtaLink>
          <CtaLink href={bookingCta.secondaryAction.href} variant="glass" size="lg">
            {bookingCta.secondaryAction.label}
          </CtaLink>
        </div>
      </SectionContainer>
    </section>
  );
}
