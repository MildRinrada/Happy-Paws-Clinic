import { CtaLink } from '@/components/ui/cta-link';
import { serviceCard } from '../../content/services-page';
import type { PublicService } from '../../types/service.types';

/** "จองบริการนี้": opens booking with this service already chosen. */
export function BookServiceLink({ service }: { service: PublicService }) {
  return (
    <CtaLink href={serviceCard.bookHref(service.id)} variant="forest" className="gap-2">
      {serviceCard.book}
      <span aria-hidden="true">→</span>
      <span className="sr-only"> {service.name}</span>
    </CtaLink>
  );
}
