import { ImageSlot } from '@/components/shared/image-slot';
import { locationMap } from '../../content/location';

const frame = 'h-64 w-full lg:h-full lg:min-h-[340px]';

/**
 * The map beside the address: a live Google Maps embed when the clinic's map entry has one,
 * otherwise a picture of the map, otherwise an empty slot.
 */
export function LocationMap({ embedUrl }: { embedUrl?: string }) {
  if (embedUrl) {
    return (
      <iframe
        src={embedUrl}
        title={locationMap.alt}
        loading="lazy"
        referrerPolicy="no-referrer-when-downgrade"
        className={`border-0 ${frame}`}
      />
    );
  }

  return (
    <ImageSlot
      src={locationMap.imageSrc}
      alt={locationMap.alt}
      label="แผนที่ (Google Maps)"
      sizes="(min-width: 1024px) 40vw, 100vw"
      className={frame}
    />
  );
}
