import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { AllergiesCard } from '@/features/pets/components/profile/allergies-card';
import { OwnersCard } from '@/features/pets/components/profile/owners-card';
import { PetFacts } from '@/features/pets/components/profile/pet-facts';
import { TimelineCard } from '@/features/pets/components/profile/timeline-card';
import { VaccinesCard } from '@/features/pets/components/profile/vaccines-card';
import { WeightCard } from '@/features/pets/components/profile/weight-card';
import { petProfile } from '@/features/pets/content/pet-profile';
import { loadPetProfile, type PetProfile } from '@/features/pets/lib/load-pet-profile';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { formatLongDate } from '@/lib/format/datetime';

export const metadata: Metadata = { title: petProfile.metaTitle };

/**
 * One patient, as the vet and the counter look them up: who they are, what they must not be
 * given, how their weight has gone, what vaccine is due, and everything the clinic has done.
 */
export default async function PetPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ cursor?: string }>;
}) {
  const [context, { id }, { cursor }] = await Promise.all([loadStaffContext(), params, searchParams]);
  const text = petProfile;

  if (!context?.can(Scope.PatientRead)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  let profile: PetProfile;
  try {
    profile = await authed(() => loadPetProfile(id, cursor));
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return <Alert>{text.notFound}</Alert>;
    if (error instanceof ApiError && error.status === 403) return <Alert>{error.message}</Alert>;
    throw error;
  }

  const { pet, allergies, latest_weight } = profile.detail;

  return (
    <div className="space-y-6">
      <PageHeader
        title={
          <span className="flex flex-wrap items-center gap-2">
            {pet.name}
            <span className="rounded-md bg-ink-100 px-2 py-0.5 font-mono text-sm font-normal text-ink-700">{pet.hospital_number}</span>
            {pet.is_deceased && pet.deceased_at && <Badge>{text.deceased(formatLongDate(pet.deceased_at))}</Badge>}
          </span>
        }
        subtitle={[pet.species, pet.breed].filter(Boolean).join(' / ')}
      />

      <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
        <div className="min-w-0 space-y-6">
          <AllergiesCard petId={pet.id} allergies={allergies} canRecord={context.can(Scope.RecordWrite)} />
          <TimelineCard petId={pet.id} history={profile.history} paged={Boolean(cursor)} />
        </div>

        <div className="space-y-6">
          <PetFacts pet={pet} />
          <OwnersCard owners={profile.owners} />
          <WeightCard latest={latest_weight} series={profile.weights} />
          {profile.vaccinations && <VaccinesCard vaccinations={profile.vaccinations} />}
        </div>
      </div>
    </div>
  );
}
