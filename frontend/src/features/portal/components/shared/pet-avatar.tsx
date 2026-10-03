/* eslint-disable @next/next/no-img-element -- photo_url is a short-lived signed link from the API, not a file next/image can optimise. */

/** A pet's photo, or the first letter of its name on the clinic's green when there is none. */
export function PetAvatar({ name, photoUrl, size = 'md' }: { name: string; photoUrl?: string | null; size?: 'md' | 'lg' }) {
  const box = size === 'lg' ? 'size-16 text-2xl' : 'size-11 text-lg';

  if (photoUrl) {
    return <img src={photoUrl} alt="" className={`${box} shrink-0 rounded-full object-cover`} />;
  }

  return (
    <span aria-hidden="true" className={`${box} flex shrink-0 items-center justify-center rounded-full bg-brand-100 font-bold text-site-teal`}>
      {name.trim().charAt(0) || '?'}
    </span>
  );
}
