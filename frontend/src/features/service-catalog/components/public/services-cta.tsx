import { CtaBand } from '@/components/site/cta-band';
import { CtaLink } from '@/components/ui/cta-link';
import { servicesCta } from '../../content/services-page';
import type { AskChannels } from '../../types/ask-channels.types';

/**
 * The close of the page: chat on LINE or ring the clinic for help choosing. A button whose contact
 * the clinic has not set is left out; with neither set, one button goes to the contact page.
 */
export function ServicesCta({ channels }: { channels: AskChannels }) {
  const { line, phone } = channels;

  return (
    <CtaBand
      variant="inset"
      heading={servicesCta.heading}
      lead={servicesCta.lead}
      className="pb-16 pt-10"
      actions={
        <>
          {line && (
            <CtaLink href={line.href} variant="violet" size="lg">
              {servicesCta.line}
            </CtaLink>
          )}
          {phone && (
            <CtaLink href={phone.href} variant="dark" size="lg">
              {servicesCta.call(phone.number)}
            </CtaLink>
          )}
          {!line && !phone && (
            <CtaLink href={servicesCta.fallback.href} variant="violet" size="lg">
              {servicesCta.fallback.label}
            </CtaLink>
          )}
        </>
      }
    />
  );
}
