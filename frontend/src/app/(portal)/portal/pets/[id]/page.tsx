import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { PetDetail } from '@/features/portal/components/pets/detail/pet-detail';
import { portalPets } from '@/features/portal/content/portal-pets';
import { loadPetDetail } from '@/features/portal/lib/load-pet-detail';

export const metadata: Metadata = { title: `${portalPets.title} | Happy Paws Clinic` };

/**
 * One pet's page: its profile, latest weight and weight chart, vaccines, visits and next
 * appointment. The portal home's pet tiles and the list of pets open it. A pet that is not on this
 * account is a 404, the same answer the backend gives, so the page does not confirm that the id exists.
 */
export default async function PortalPetPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const view = await loadPetDetail(id);

  if (!view) notFound();

  return (
    <div className="font-sans">
      <PetDetail view={view} />
    </div>
  );
}
