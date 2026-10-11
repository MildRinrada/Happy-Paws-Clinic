import Link from 'next/link';
import { portalPaths } from '../../../content/portal-navigation';
import { portalPets } from '../../../content/portal-pets';
import { recordsHref } from '../../../lib/portal-links';
import type { MyPet } from '../../../types/portal.types';
import { outlineButton } from '../pet-button-styles';

/**
 * The buttons under the visits on a phone: "ดูประวัติการรักษา" (only when this owner may see the
 * pet's history), "แก้ไขข้อมูล" (not for a pet that has died) and "เอกสารของน้อง". Two to a row;
 * an odd last one takes the whole row. From 1024px these live on the profile card and the card's title.
 */
export function VisitCardActions({ pet }: { pet: MyPet }) {
  const links = [
    pet.may_view_history && { label: portalPets.records, href: recordsHref(pet.id) },
    !pet.is_deceased && { label: portalPets.edit, href: portalPaths.editPet(pet.id) },
    { label: portalPets.documents, href: portalPaths.petDocuments(pet.id) },
  ].filter((link): link is { label: string; href: string } => Boolean(link));

  return (
    <div className="grid grid-cols-2 gap-2.5 pb-4 pt-1 lg:hidden">
      {links.map((link) => (
        <Link key={link.href} href={link.href} className={`${outlineButton} [&:last-child:nth-child(odd)]:col-span-2`}>
          {link.label}
        </Link>
      ))}
    </div>
  );
}
