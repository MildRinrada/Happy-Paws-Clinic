import { PageBanner } from '@/components/site/page-banner';
import { CtaLink } from '@/components/ui/cta-link';
import type { PageBannerData } from '@/features/site-banners/types/page-banner.types';
import { servicesBannerSlotLabel } from '../../content/services-page';

/**
 * The top of the page: what it is for, two buttons, and a picture with the four circles. Every word
 * and the picture come in `banner`, from `loadPageBanner('services')`.
 */
export function ServicesBanner({ banner }: { banner: PageBannerData }) {
  return (
    <PageBanner
      size="feature"
      eyebrow={banner.eyebrow}
      heading={banner.heading}
      lead={banner.lead}
      image={{ src: banner.image?.src, alt: banner.image?.alt ?? '', label: servicesBannerSlotLabel }}
      dots
      actions={
        <>
          <CtaLink href={banner.primaryAction.href} variant="solidInk" size="lg" className="sm:min-w-[168px]">
            {banner.primaryAction.label}
          </CtaLink>
          {banner.secondaryAction && (
            <CtaLink href={banner.secondaryAction.href} variant="outline" size="lg" className="sm:min-w-[148px]">
              {banner.secondaryAction.label}
            </CtaLink>
          )}
        </>
      }
    />
  );
}
