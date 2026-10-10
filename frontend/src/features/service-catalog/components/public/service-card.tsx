import { CtaLink } from '@/components/ui/cta-link';
import { ServiceTag } from '@/features/home/components/services/service-tag';
import { serviceCard } from '../../content/services-page';
import { formatDuration } from '../../lib/service-price';
import type { PublicService } from '../../types/service.types';
import { PriceTierList } from './price-tier-list';
import { ServicePriceLabel } from './service-price-label';

interface Props {
  service: PublicService;
  branchNames: Record<string, string>;
}

/**
 * One service: name and price, how long it takes, which branches have it, prices by species and
 * weight when there are some, and a button that opens booking with this service already chosen.
 */
export function ServiceCard({ service, branchNames }: Props) {
  const branches = service.branch_ids.map((id) => branchNames[id]).filter(Boolean);

  return (
    // A column, so the buttons sit on the cards' bottom edge and line up across a row.
    <li className="flex flex-col rounded-2xl border border-site-border bg-white p-5">
      <div className="flex items-start justify-between gap-4">
        <h3 className="text-lg font-bold leading-tight">{service.name}</h3>
        <ServicePriceLabel service={service} />
      </div>

      <ul className="mt-3 flex flex-wrap gap-2">
        <ServiceTag tag={{ label: formatDuration(service.duration_minutes), tone: 'neutral' }} />
        {branches.length > 0 && (
          <ServiceTag tag={{ label: `${serviceCard.branchesOnly} ${branches.join(', ')}`, tone: 'notice' }} />
        )}
      </ul>

      {service.price_tiers.length > 0 && <PriceTierList service={service} />}

      <div className="mt-auto pt-5">
        <CtaLink href={serviceCard.bookHref(service.id)} className="w-full">
          {serviceCard.book}
          <span className="sr-only"> {service.name}</span>
        </CtaLink>
      </div>
    </li>
  );
}
