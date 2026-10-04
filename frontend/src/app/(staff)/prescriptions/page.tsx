import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { buttonClass } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext, type StaffContext } from '@/features/account/lib/staff-context';
import { searchPets } from '@/features/pets/api/searchPets';
import { DispenseQueue } from '@/features/prescriptions/components/dispense-queue';
import { PetBanner } from '@/features/prescriptions/components/pet-banner';
import { PetSearchForm } from '@/features/prescriptions/components/pet-search-form';
import { PetSearchResults } from '@/features/prescriptions/components/pet-search-results';
import { PrescriptionHistory } from '@/features/prescriptions/components/prescription-history';
import { prescriptionsPage } from '@/features/prescriptions/content/prescriptions-page';
import { loadDispenseQueue, loadPetPrescriptions } from '@/features/prescriptions/lib/load-prescriptions-view';
import { prescriptionPaths } from '@/features/prescriptions/lib/prescription-paths';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: prescriptionsPage.metaTitle };

type Params = { pet_id?: string; q?: string; cursor?: string };

/**
 * The pharmacy counter. With `?pet_id=` it is one patient's medication history, still to hand over
 * first, each entry checked for duplicates, with the dispense button and the label. Without it,
 * a pet search and the branch's prescriptions waiting to be dispensed.
 */
export default async function PrescriptionsPage({ searchParams }: { searchParams: Promise<Params> }) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.PrescriptionRead)) {
    return <Alert tone="info">{prescriptionsPage.noAccess}</Alert>;
  }

  const petId = params.pet_id?.trim();

  try {
    return petId ? await petView(context, petId, params.cursor) : await startView(context, params);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return <Alert>{prescriptionsPage.petNotFound}</Alert>;
    }
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }
}

async function petView(context: StaffContext, petId: string, cursor?: string) {
  const text = prescriptionsPage;
  const view = await authed(() => loadPetPrescriptions(petId, cursor));

  return (
    <div className="space-y-6">
      <div className="no-print space-y-3">
        <BackLink href={prescriptionPaths.list}>{text.pet.back}</BackLink>
        <PageHeader title={text.title} subtitle={view.pet ? undefined : petId} />
        {view.pet && <PetBanner detail={view.pet} />}
      </div>

      <Card className="print:border-0">
        <div className="no-print">
          <CardHeader title={text.pet.heading} />
        </div>
        <CardBody className="print:p-0">
          <PrescriptionHistory
            items={view.items}
            canDispense={context.can(Scope.PrescriptionDispense)}
            canOpenCase={context.can(Scope.ConsultRead)}
          />
        </CardBody>
      </Card>

      {view.nextCursor && (
        <Link
          href={`${prescriptionPaths.forPet(petId)}&cursor=${encodeURIComponent(view.nextCursor)}`}
          className={buttonClass('secondary', 'sm', 'no-print')}
        >
          {text.next}
        </Link>
      )}
    </div>
  );
}

async function startView(context: StaffContext, params: Params) {
  const text = prescriptionsPage;
  const query = (params.q ?? '').trim().slice(0, 100);
  const canSearch = context.can(Scope.PatientRead);

  const [pets, queue] = await Promise.all([
    canSearch && query ? authed(() => searchPets(query)) : Promise.resolve(null),
    authed(() => loadDispenseQueue(params.cursor)),
  ]);

  return (
    <div className="space-y-6">
      <PageHeader title={text.title} subtitle={text.subtitle} />

      {canSearch ? <PetSearchForm query={query} /> : <Alert tone="info">{text.search.noScope}</Alert>}

      {pets && <PetSearchResults query={query} pets={pets} />}

      <DispenseQueue rows={queue.rows} />

      {queue.nextCursor && (
        <Link
          href={`${prescriptionPaths.list}?${new URLSearchParams({ ...(query && { q: query }), cursor: queue.nextCursor })}`}
          className={buttonClass('secondary', 'sm')}
        >
          {text.next}
        </Link>
      )}
    </div>
  );
}
