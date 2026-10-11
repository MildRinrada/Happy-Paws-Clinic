import { ImageSlot } from '@/components/shared/image-slot';
import { BannerDots } from './banner-dots';

/** A banner's picture. Without `src` an empty slot is drawn, with `label` in it. */
export interface BannerImageData {
  src?: string;
  alt: string;
  /** What picture belongs here, shown while there is none. */
  label: string;
}

/** How the empty slot is drawn: see-through on a dark band, light grey on a light one. */
export type BannerImageTone = 'onDark' | 'onLight';

/** The picture at the banner's right, cropped to fill a rounded frame, with the decorative circles when asked for. */
export function BannerImage({ image, dots, tone = 'onDark' }: { image: BannerImageData; dots: boolean; tone?: BannerImageTone }) {
  return (
    <div className="relative">
      <ImageSlot
        src={image.src}
        alt={image.alt}
        label={image.label}
        tone={tone}
        priority
        sizes="(min-width: 1024px) 440px, 100vw"
        className="aspect-[23/16] w-full overflow-hidden rounded-2xl"
      />
      {dots && <BannerDots />}
    </div>
  );
}
