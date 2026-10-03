import Image from 'next/image';

interface Props {
  /** Path under /public. Leave it out and the slot is drawn empty, with `label` inside. */
  src?: string;
  alt: string;
  /** Says what picture belongs here, for whoever is filling the page in. */
  label: string;
  /** Size and shape of the slot. It must give the slot a height — the picture fills it. */
  className?: string;
  /** How the picture sits in the slot. Defaults to covering it. */
  imageClassName?: string;
  /** `onDark` when the slot sits on a coloured section, so the empty outline stays visible. */
  tone?: 'onLight' | 'onDark';
  /** Passed to next/image so the browser downloads a file near the size it will paint. */
  sizes?: string;
  priority?: boolean;
}

const emptyTones = {
  onLight: 'border-ink-300 bg-ink-100 text-ink-500',
  onDark: 'border-white/50 bg-white/10 text-white/80',
};

/**
 * A place on the page for a picture that may not have been supplied yet.
 *
 * The page is laid out around the slot, not around the picture, so dropping the file in later
 * changes one `src` and moves nothing.
 */
export function ImageSlot({
  src,
  alt,
  label,
  className = '',
  imageClassName = 'object-cover',
  tone = 'onLight',
  sizes = '100vw',
  priority = false,
}: Props) {
  if (!src) {
    return (
      <div
        aria-hidden="true"
        className={`flex items-center justify-center border border-dashed px-4 text-center text-sm ${emptyTones[tone]} ${className}`}
      >
        {label}
      </div>
    );
  }

  return (
    <div className={`relative ${className}`}>
      <Image src={src} alt={alt} fill sizes={sizes} priority={priority} className={imageClassName} />
    </div>
  );
}
