import { CtaBand } from '@/components/site/cta-band';
import { CtaLink } from '@/components/ui/cta-link';
import { contactPage } from '../content/contact-page';

/** The close of the page: book a visit. */
export function ContactCta() {
  const { cta } = contactPage;

  return (
    <CtaBand
      variant="slim"
      heading={cta.heading}
      lead={cta.lead}
      actions={
        <CtaLink href={cta.book.href} variant="violet" size="lg">
          {cta.book.label}
        </CtaLink>
      }
    />
  );
}
