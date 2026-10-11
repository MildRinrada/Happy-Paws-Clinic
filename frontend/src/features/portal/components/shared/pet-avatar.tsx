/* eslint-disable @next/next/no-img-element -- photo_url is a short-lived signed link from the API, not a file next/image can optimise. */

/** The circle's colour when there is no photo. `brand` is the portal's usual; the others are for a row of pets. */
export type PetAvatarTone = 'brand' | 'mint' | 'sun' | 'white';

const tones: Record<PetAvatarTone, string> = {
  brand: 'bg-brand-100 text-site-teal',
  mint: 'bg-brand-100 text-brand-800',
  sun: 'bg-site-sun text-ink-900',
  /** For a pet whose colour is mint, drawn on a mint band where mint would not show. */
  white: 'bg-white text-brand-800',
};

const sizes = {
  /** In a pet switcher chip. */
  chip: 'size-7 text-sm',
  md: 'size-11 text-lg',
  tile: 'size-12 text-xl',
  lg: 'size-16 text-2xl',
  /** At the top of a pet's profile card: 76px on a phone, 80px from 1024px. */
  hero: 'size-[76px] text-[32px] lg:size-20 lg:text-[34px]',
};

interface PetAvatarProps {
  name: string;
  photoUrl?: string | null;
  size?: keyof typeof sizes;
  tone?: PetAvatarTone;
  /** A white ring around the circle, for an avatar on a coloured band. */
  ring?: boolean;
}

/** A pet's photo, or the first letter of its name on a coloured circle when there is none. */
export function PetAvatar({ name, photoUrl, size = 'md', tone = 'brand', ring = false }: PetAvatarProps) {
  const box = `${sizes[size]}${ring ? ' ring-[3px] ring-white lg:ring-4' : ''}`;

  if (photoUrl) {
    return <img src={photoUrl} alt="" className={`${box} shrink-0 rounded-full object-cover`} />;
  }

  return (
    <span
      aria-hidden="true"
      className={`${box} flex shrink-0 items-center justify-center rounded-full font-display font-bold ${tones[tone]}`}
    >
      {name.trim().charAt(0) || '?'}
    </span>
  );
}
