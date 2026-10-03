import { ImageSlot } from '@/components/shared/image-slot';
import type { FeaturedService } from '../../types/home.types';
import { ServiceTag } from './service-tag';

/** A large service card: picture, name and starting price, a line about it, and its tags. */
export function FeaturedServiceCard({ service }: { service: FeaturedService }) {
  return (
    // A column, so the tags sit on the card's bottom edge and line up across a row of cards
    // whatever the length of each description.
    <li className="flex flex-col overflow-hidden rounded-2xl border border-site-border bg-white">
      <ImageSlot
        src={service.image.src}
        alt={service.image.alt}
        label={`รูป ${service.name}`}
        sizes="(min-width: 1024px) 30vw, 100vw"
        className="h-28 shrink-0 border-x-0 border-t-0"
      />

      <div className="flex flex-1 flex-col px-5 pb-5 pt-4">
        <div className="flex items-baseline justify-between gap-4">
          <h3 className="text-lg font-bold leading-tight">{service.name}</h3>
          <p className="shrink-0 text-sm font-bold text-site-teal">{service.price}</p>
        </div>

        <p className="mt-1.5 text-[13px] leading-[17px] text-[#555555]">
          {service.description.map((line) => (
            <span key={line} className="block">
              {line}
            </span>
          ))}
        </p>

        <ul className="mt-auto flex flex-wrap gap-x-3 gap-y-2 pt-4">
          {service.tags.map((tag) => (
            <ServiceTag key={tag.label} tag={tag} />
          ))}
        </ul>
      </div>
    </li>
  );
}
