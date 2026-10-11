import { CtaBand } from '@/components/site/cta-band';
import { CtaLink } from '@/components/ui/cta-link';
import { bookingCta } from '../../content/booking-cta';

/** The last thing on the page: an invitation to book, with sign-in beside it. */
export function BookingCtaSection() {
  return (
    <CtaBand
      heading={bookingCta.heading}
      lead={bookingCta.lead}
      actionsClassName="lg:gap-[22px]"
      actions={
        <>
          <CtaLink href={bookingCta.primaryAction.href} variant="violet" size="lg">
            {bookingCta.primaryAction.label}
          </CtaLink>
          <CtaLink href={bookingCta.secondaryAction.href} variant="glass" size="lg">
            {bookingCta.secondaryAction.label}
          </CtaLink>
        </>
      }
    />
  );
}
