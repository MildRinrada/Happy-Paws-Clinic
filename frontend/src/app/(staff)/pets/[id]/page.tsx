import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { loadVets } from '@/features/appointments/lib/load-calendar';
import { AllergiesCard } from '@/features/pets/components/profile/allergies-card';
import { DocumentsCard } from '@/features/pets/components/profile/documents-card';
import { EditPetCard } from '@/features/pets/components/profile/edit-pet-card';
import { OwnersCard } from '@/features/pets/components/profile/owners-card';
import { PetFacts } from '@/features/pets/components/profile/pet-facts';
import { RecordDeathCard } from '@/features/pets/components/profile/record-death-card';
import { TimelineCard } from '@/features/pets/components/profile/timeline-card';
import { TreatmentPlanCard } from '@/features/pets/components/profile/treatment-plan-card';
import { UndoDeathCard } from '@/features/pets/components/profile/undo-death-card';
import { VaccinesCard } from '@/features/pets/components/profile/vaccines-card';
import { WeightCard } from '@/features/pets/components/profile/weight-card';
import { petProfile } from '@/features/pets/content/pet-profile';
import { recordDeath } from '@/features/pets/content/record-death';
import { loadPetProfile, type PetProfile } from '@/features/pets/lib/load-pet-profile';
import { prescriptionPaths } from '@/features/prescriptions/lib/prescription-paths';
import { listServices } from '@/features/service-catalog/api/listServices';
import { PetSummaryCard } from '@/features/staff-ai/components/pet-summary/pet-summary-card';
import { loadAiUsage } from '@/features/staff-ai/lib/load-ai-usage';
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

  // A closed record takes no new dose and no new plan (the backend refuses both), and no new
  // allergy or carer: those forms are hidden too, as nothing they record can matter any more.
  const alive = !pet.is_deceased;
  const canRecord = context.can(Scope.RecordWrite);
  const canWritePatient = context.can(Scope.PatientWrite);
  const canPlan = alive && context.can(Scope.ConsultWrite);
  const canPrint = context.can(Scope.RecordRead);
  // "สรุปด้วย AI" reads the pet's file: record:read, and the assistant on for this session (§ 34).
  const canSummarize = canPrint && (await loadAiUsage()) !== null;
  const branchId = context.branch?.id;

  // What the plan form picks from, read only for someone who may plan.
  const [services, vets] = canPlan && branchId
    ? await Promise.all([listServices(branchId).catch(() => []), loadVets(branchId)])
    : [[], []];

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
        actions={
          context.can(Scope.PrescriptionRead) && (
            <Link
              href={prescriptionPaths.forPet(pet.id)}
              className="rounded-lg border border-ink-200 bg-white px-3 py-1.5 text-sm font-medium text-ink-700 hover:bg-ink-50"
            >
              {text.drugHistory}
            </Link>
          )
        }
      />

      {pet.is_deceased && <Alert tone="info">{recordDeath.closedNote}</Alert>}

      <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
        <div className="min-w-0 space-y-6">
          <AllergiesCard petId={pet.id} allergies={allergies} canRecord={alive && canRecord} />
          {canSummarize && <PetSummaryCard petId={pet.id} />}
          {canPlan && <TreatmentPlanCard petId={pet.id} branchId={branchId} services={services} vets={vets} />}
          <TimelineCard petId={pet.id} history={profile.history} paged={Boolean(cursor)} />
        </div>

        <div className="space-y-6">
          <PetFacts pet={pet} />
          {canWritePatient && <EditPetCard pet={pet} />}
          <OwnersCard petId={pet.id} owners={profile.owners} canManage={alive && canWritePatient} />
          <WeightCard latest={latest_weight} series={profile.weights} />
          {profile.vaccinations && (
            <VaccinesCard petId={pet.id} vaccinations={profile.vaccinations} canRecord={alive && canRecord} />
          )}
          {(canRecord || canPrint) && <DocumentsCard petId={pet.id} canUpload={canRecord} canPrint={canPrint} />}
          {canWritePatient && <RecordDeathCard petId={pet.id} deceased={pet.is_deceased} />}
          {canWritePatient && <UndoDeathCard petId={pet.id} deceased={pet.is_deceased} />}
        </div>
      </div>
    </div>
  );
}
