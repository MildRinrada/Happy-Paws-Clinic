import { PageBanner } from '@/components/site/page-banner';
import { CtaLink } from '@/components/ui/cta-link';
import type { PageBannerData } from '@/features/site-banners/types/page-banner.types';
import { teamPage } from '../content/team-page';

/**
 * The top of the page on a light grey band: what the team is, a button to book, and a picture of
 * the team (an empty slot until there is one). Every word and the picture come in `banner`, from
 * `loadPageBanner('team')`.
 */
export function TeamBanner({ banner }: { banner: PageBannerData }) {
  return (
    <PageBanner
      size="feature"
      tone="soft"
      eyebrow={banner.eyebrow}
      heading={banner.heading}
      lead={banner.lead}
      image={{ src: banner.image?.src, alt: banner.image?.alt ?? '', label: teamPage.bannerSlotLabel }}
      actions={
        <>
          <CtaLink href={banner.primaryAction.href} variant="solidInk" size="lg" className="sm:min-w-[180px]">
            {banner.primaryAction.label}
          </CtaLink>
          {banner.secondaryAction && (
            <CtaLink href={banner.secondaryAction.href} variant="outlineForest" size="lg">
              {banner.secondaryAction.label}
            </CtaLink>
          )}
        </>
      }
    />
  );
}
