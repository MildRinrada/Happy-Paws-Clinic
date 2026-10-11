import { SiteCard } from '@/components/site/site-card';
import type { MoreService } from '../../types/home.types';
import { ServiceIcon } from './service-icon';

/** A compact service card: an icon, the name, and one line with the starting price. */
export function MoreServiceCard({ service }: { service: MoreService }) {
  return (
    <SiteCard as="li" radius="md" padding="none" className="flex min-h-16 items-center gap-3.5 py-2 pl-5 pr-3">
      <ServiceIcon name={service.icon} className="size-8 shrink-0" />
      <div>
        <h3 className="text-lg font-bold leading-tight">{service.name}</h3>
        <p className="mt-0.5 text-[13px] leading-tight text-[#555555]">{service.detail}</p>
      </div>
    </SiteCard>
  );
}
