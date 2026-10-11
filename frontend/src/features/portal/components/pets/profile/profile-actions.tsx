import Link from 'next/link';
import { portalPaths } from '../../../content/portal-navigation';
import { portalPets } from '../../../content/portal-pets';
import type { MyPet } from '../../../types/portal.types';
import { outlineButton } from '../pet-button-styles';

/**
 * "แก้ไขข้อมูล" and "เอกสารของน้อง" at the foot of the card, from 1024px (a phone has them under
 * the visits). A pet that has died is no longer edited, so it keeps only its documents.
 */
export function ProfileActions({ pet }: { pet: MyPet }) {
  return (
    <div className="hidden gap-2.5 px-5 pb-5 pt-2 lg:flex">
      {!pet.is_deceased && (
        <Link href={portalPaths.editPet(pet.id)} className={`${outlineButton} flex-1`}>
          {portalPets.edit}
        </Link>
      )}
      <Link href={portalPaths.petDocuments(pet.id)} className={`${outlineButton} flex-1`}>
        {portalPets.documents}
      </Link>
    </div>
  );
}
