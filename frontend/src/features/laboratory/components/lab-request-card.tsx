import Link from 'next/link';
import { Card, CardBody } from '@/components/ui/card';
import { consultationPaths } from '@/features/consultations/lib/consultation-paths';
import { petPaths } from '@/features/pets/lib/pet-paths';
import { labPage } from '../content/lab-page';
import type { LabRow } from '../lib/load-lab-worklist';
import { LabRequestActions } from './lab-request-actions';
import { LabRequestSummary } from './lab-request-summary';
import { LabResultTable } from './lab-result-table';

interface LabRequestCardProps {
  row: LabRow;
  canRecord: boolean;
  canReview: boolean;
  /** `consult:read`: the link back to the case the sample was taken in. */
  canOpenCase: boolean;
}

/** One request on the worklist: the patient, what was asked for, the results so far, and what to do next. */
export function LabRequestCard({ row, canRecord, canReview, canOpenCase }: LabRequestCardProps) {
  const { request, pet } = row;
  const text = labPage.request;

  return (
    <Card>
      <CardBody className="space-y-3">
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          <p className="font-medium text-ink-900">
            {pet ? pet.name : request.pet_id}
            {pet && <span className="ml-2 rounded-md bg-ink-100 px-1.5 py-0.5 font-mono text-xs text-ink-700">{pet.hospitalNumber}</span>}
          </p>
          <div className="flex flex-wrap gap-3 text-sm">
            {canOpenCase && request.consultation_id && (
              <Link href={consultationPaths.detail(request.consultation_id)} className="font-medium text-brand-700 hover:underline">
                {text.openCase}
              </Link>
            )}
            {pet && (
              <Link href={petPaths.detail(request.pet_id)} className="font-medium text-brand-700 hover:underline">
                {text.openPet}
              </Link>
            )}
          </div>
        </div>
        <LabRequestSummary request={request} />
        {request.results.length > 0 && <LabResultTable results={request.results} />}
        <LabRequestActions request={request} canRecord={canRecord} canReview={canReview} />
      </CardBody>
    </Card>
  );
}
