import type { Metadata } from 'next';
import { redirect } from 'next/navigation';
import { PetDetail } from '@/features/portal/components/pets/detail/pet-detail';
import { PetList } from '@/features/portal/components/pets/list/pet-list';
import { PetsHeading } from '@/features/portal/components/pets/pets-heading';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { portalPaths } from '@/features/portal/content/portal-navigation';
import { portalPets } from '@/features/portal/content/portal-pets';
import { loadPetDetail } from '@/features/portal/lib/load-pet-detail';
import { loadPortalHome } from '@/features/portal/lib/load-portal-home';
import { firstPet } from '@/features/portal/lib/order-pets';
import { petListItems } from '@/features/portal/lib/pet-list-items';

export const metadata: Metadata = { title: `${portalPets.title} | Happy Paws Clinic` };

/**
 * "สัตว์เลี้ยงของฉัน". On a phone, the list of pets (living first, a search box for a long list,
 * those that have died folded away), each card opening the pet's page. From 1024px the page is the
 * first living pet's own page, as the desktop design draws this menu item, and the chips at the top
 * reach the others. An account with a single pet goes straight to that pet's page.
 *
 * The pets come from `GET /portal/home`, which the layout has already read for the bell.
 */
export default async function PortalPetsPage() {
  const home = await loadPortalHome();
  const first = firstPet(home.pets);

  if (!first) {
    return (
      <div className="space-y-5 font-sans">
        <PetsHeading />
        <EmptyLine>{portalPets.empty}</EmptyLine>
      </div>
    );
  }

  if (home.pets.length === 1) redirect(portalPaths.pet(first.id));

  const detail = await loadPetDetail(first.id);

  return (
    <div className="font-sans">
      <div className="space-y-5 lg:hidden">
        <PetsHeading />
        <PetList pets={petListItems(home.pets, home.vaccinations_due)} />
      </div>

      {detail && (
        <div className="hidden lg:block">
          <PetDetail view={detail} />
        </div>
      )}
    </div>
  );
}
