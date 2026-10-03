import { ImageSlot } from '@/components/shared/image-slot';
import { footerAddress } from '@/config/site-footer';
import type { ClinicContact } from '@/features/clinic/types/contact.types';
import { SiteLogo } from '../site-header/site-logo';
import { FooterColumn } from './footer-column';

/**
 * The first column: the logo, the address, a small map and the way to open it. The address and
 * link are the clinic's map entry; with a Google Maps embed the map is live, otherwise a picture.
 */
export function FooterBrandColumn({ map }: { map?: ClinicContact }) {
  return (
    <FooterColumn heading={<SiteLogo tone="light" />}>
      {map && <address className="whitespace-pre-line text-[13px] not-italic leading-[17px]">{map.value}</address>}

      {map?.embed_url ? (
        <iframe
          src={map.embed_url}
          title={footerAddress.map.alt}
          loading="lazy"
          referrerPolicy="no-referrer-when-downgrade"
          className="mt-[18px] aspect-[339/168] w-full max-w-[253px] rounded-2xl border-0"
        />
      ) : (
        <ImageSlot
          src={footerAddress.map.imageSrc}
          alt={footerAddress.map.alt}
          label="แผนที่"
          tone="onDark"
          sizes="260px"
          className="mt-[18px] aspect-[339/168] w-full max-w-[253px] overflow-hidden rounded-2xl"
        />
      )}

      {map?.url && (
        <a
          href={map.url}
          target="_blank"
          rel="noreferrer"
          className="mt-[18px] inline-flex h-10 items-center rounded-lg bg-site-footer-raised px-3.5 text-[13px] transition hover:bg-white/25"
        >
          {footerAddress.mapLinkLabel}
        </a>
      )}
    </FooterColumn>
  );
}
