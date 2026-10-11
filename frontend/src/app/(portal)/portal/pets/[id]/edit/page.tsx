import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { listMyPets } from '@/features/portal/api/listMyPets';
import { PetEditForm } from '@/features/portal/components/pets/pet-edit-form';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { petEdit } from '@/features/portal/content/pet-edit';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: 'แก้ไขข้อมูลสัตว์เลี้ยง | Happy Paws Clinic' };

/**
 * Editing the owner's side of one pet. A pet that is not on this account is a 404, the same answer
 * the backend gives, so the page does not confirm that the id exists.
 */
export default async function EditPetPage({ params }: { params: Promise<{ id: string }> }) {
  const [{ id }, pets] = await Promise.all([params, authed(listMyPets)]);
  const pet = pets.find((item) => item.id === id);

  if (!pet) {
    notFound();
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <PageHeading title={petEdit.title(pet.name)} intro={petEdit.intro} />

      <div className="rounded-2xl bg-white p-5 shadow-sm sm:p-6">
        {pet.is_deceased ? <EmptyLine>{petEdit.deceased}</EmptyLine> : <PetEditForm pet={pet} />}
      </div>
    </div>
  );
}
