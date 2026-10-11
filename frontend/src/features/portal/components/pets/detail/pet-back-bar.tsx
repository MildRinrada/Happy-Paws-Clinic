import Link from 'next/link';
import { petDetail } from '../../../content/pet-detail';
import { portalPets } from '../../../content/portal-pets';
import { ChevronIcon } from '../chevron-icon';

/**
 * The white bar under the header on a phone: a back arrow to the list of pets and the page's name.
 * It reaches the screen's edges over the page's own margin. From 1024px the title row and the pet
 * chips take its place.
 */
export function PetBackBar({ href }: { href: string }) {
  return (
    <div className="-mx-4 -mt-6 mb-4 flex h-14 items-center gap-1 border-b border-ink-200 bg-white px-2 sm:-mx-6 sm:px-4 lg:hidden">
      <Link
        href={href}
        aria-label={petDetail.back}
        className="flex size-10 items-center justify-center rounded-full text-ink-800 hover:bg-ink-100"
      >
        <ChevronIcon direction="left" className="size-6" />
      </Link>
      <h1 className="font-display text-[17px] font-semibold text-ink-900">{portalPets.title}</h1>
    </div>
  );
}
