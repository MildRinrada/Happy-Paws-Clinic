import type { Metadata } from 'next';
import { getPetRecords } from '@/features/portal/api/getPetRecords';
import { PetPicker } from '@/features/portal/components/records/pet-picker';
import { PetRecords } from '@/features/portal/components/records/pet-records';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { portalRecords } from '@/features/portal/content/portal-records';
import { loadPortalHome } from '@/features/portal/lib/load-portal-home';
import { optional } from '@/lib/api/api-error';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: `${portalRecords.title} · Happy Paws Clinic` };

interface RecordsPageProps {
  searchParams: Promise<{ pet?: string }>;
}

/**
 * One pet's history at a time, chosen with `?pet=`. Only pets whose link lets this owner see
 * history are offered; the first of them is shown when none is chosen.
 */
export default async function PortalRecordsPage({ searchParams }: RecordsPageProps) {
  const [{ pet: requested }, home] = await Promise.all([searchParams, loadPortalHome()]);
  const pets = home.pets.filter((pet) => pet.may_view_history);
  const selected = pets.find((pet) => pet.id === requested) ?? pets[0];

  const records = selected ? await authed(() => optional(getPetRecords(selected.id))) : null;

  return (
    <div className="space-y-6">
      <PageHeading title={portalRecords.title} intro={portalRecords.intro} />

      {!selected ? (
        <EmptyLine>{portalRecords.noPets}</EmptyLine>
      ) : (
        <>
          <PetPicker pets={pets} selectedId={selected.id} />
          {records ? <PetRecords records={records} /> : <EmptyLine>{portalRecords.notFound}</EmptyLine>}
        </>
      )}
    </div>
  );
}
