import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import { listMyPets } from '@/features/portal/api/listMyPets';
import { DocumentUploadForm } from '@/features/portal/components/documents/document-upload-form';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { petDocuments } from '@/features/portal/content/pet-documents';
import { portalPaths } from '@/features/portal/content/portal-navigation';
import { recordsHref } from '@/features/portal/lib/portal-links';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: `${petDocuments.metaTitle} | Happy Paws Clinic` };

/**
 * An owner sending the clinic a file for one pet: a photo of a symptom, results from another
 * clinic. A pet that is not on this account is a 404, the same answer the backend gives.
 */
export default async function PetDocumentsPage({ params }: { params: Promise<{ id: string }> }) {
  const [{ id }, pets] = await Promise.all([params, authed(listMyPets)]);
  const pet = pets.find((item) => item.id === id);

  if (!pet) {
    notFound();
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div className="space-y-2">
        <Link href={portalPaths.pet(pet.id)} className="text-sm font-medium text-site-teal hover:underline">
          {petDocuments.back}
        </Link>
        <PageHeading title={petDocuments.title(pet.name)} intro={petDocuments.intro} />
      </div>

      <div className="rounded-2xl bg-white p-5 shadow-sm sm:p-6">
        <DocumentUploadForm petId={pet.id} recordsHref={pet.may_view_history ? recordsHref(pet.id) : undefined} />
      </div>
    </div>
  );
}
