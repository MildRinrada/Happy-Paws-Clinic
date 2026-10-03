import { ImageSlot } from '@/components/shared/image-slot';
import { heroImage } from '../../content/hero';

/**
 * The banner picture. On a wide screen it stands on the hero's bottom edge and runs off the
 * right of the page; on a narrow one it sits under the text.
 *
 * It is a cut-out (no background), so it is fitted whole and anchored to the bottom rather than
 * cropped to fill.
 */
export function HeroImage() {
  return (
    <ImageSlot
      src={heroImage.src}
      alt={heroImage.alt}
      label="รูป banner (สุนัขและแมว)"
      tone="onDark"
      priority
      sizes="(min-width: 1024px) 43vw, 100vw"
      imageClassName="object-contain object-bottom"
      className="mx-5 mb-10 h-56 rounded-2xl sm:mx-6 sm:h-72 lg:absolute lg:bottom-0 lg:right-0 lg:m-0 lg:h-[82%] lg:w-[43%] lg:rounded-none lg:border-b-0 lg:border-r-0"
    />
  );
}
