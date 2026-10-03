import type { Metadata } from 'next';
import { PetProfileCard } from '@/features/portal/components/pets/pet-profile-card';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { listMyPets } from '@/features/portal/api/listMyPets';
import { portalPets } from '@/features/portal/content/portal-pets';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: `${portalPets.title} · Happy Paws Clinic` };

/** Every pet on the account, one card each. */
export default async function PortalPetsPage() {
  const pets = await authed(listMyPets);

  return (
    <div className="space-y-6">
      <PageHeading title={portalPets.title} intro={portalPets.intro} />

      {pets.length === 0 ? (
        <EmptyLine>{portalPets.empty}</EmptyLine>
      ) : (
        <div className="grid gap-6 md:grid-cols-2">
          {pets.map((pet) => (
            <PetProfileCard key={pet.id} pet={pet} />
          ))}
        </div>
      )}
    </div>
  );
}
