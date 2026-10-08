import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { ClosePanel } from '@/features/consultations/components/close/close-panel';
import { ClosedNotice } from '@/features/consultations/components/close/closed-notice';
import { ConsultationHeader } from '@/features/consultations/components/consultation-header';
import { DiagnosisForm } from '@/features/consultations/components/diagnoses/diagnosis-form';
import { DiagnosisList } from '@/features/consultations/components/diagnoses/diagnosis-list';
import { ExaminationForm } from '@/features/consultations/components/exam/examination-form';
import { NotesForm } from '@/features/consultations/components/notes/notes-form';
import { PartCard } from '@/features/consultations/components/part-card';
import { PrescriptionForm } from '@/features/consultations/components/prescriptions/prescription-form';
import { PrescriptionList } from '@/features/consultations/components/prescriptions/prescription-list';
import { TreatmentForm } from '@/features/consultations/components/treatments/treatment-form';
import { TreatmentList } from '@/features/consultations/components/treatments/treatment-list';
import { VitalsForm } from '@/features/consultations/components/vitals/vitals-form';
import { VitalsList } from '@/features/consultations/components/vitals/vitals-list';
import { consultationPage } from '@/features/consultations/content/consultation-page';
import { CLOSED_PARAMS } from '@/features/consultations/lib/closed-params';
import { loadConsultationView, type ConsultationView } from '@/features/consultations/lib/load-consultation-view';
import { CaseLabRequests } from '@/features/laboratory/components/case-lab-requests';
import { LabOrderForm } from '@/features/laboratory/components/order/lab-order-form';
import { listServices } from '@/features/service-catalog/api/listServices';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: consultationPage.metaTitle };

/**
 * The consulting room: one case, in the order a visit goes. Every part saves on its own and the
 * page re-reads the case afterwards, so what is on screen is always what the backend holds. Each
 * form appears only for a session holding its scope; the backend checks again on every write.
 */
export default async function ConsultationPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const [context, { id }, query] = await Promise.all([loadStaffContext(), params, searchParams]);
  const text = consultationPage;

  if (!context?.can(Scope.ConsultRead)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  let view: ConsultationView;
  try {
    view = await authed(() => loadConsultationView(id, { withLab: context.can(Scope.LabRead) }));
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return <Alert>{text.notFound}</Alert>;
    if (error instanceof ApiError && error.status === 403) return <Alert>{error.message}</Alert>;
    throw error;
  }

  const { consultation } = view;
  const open = consultation.status === 'draft';
  const canWrite = open && context.can(Scope.ConsultWrite);
  const canOrderLab = open && context.can(Scope.LabRequest);
  // For booking a follow-up. The only service list the contract has is the public one.
  const services = open ? ((await listServices(consultation.branch_id).catch(() => [])) ?? []) : [];
  const invoiceId = query[CLOSED_PARAMS.invoiceId];
  const invoiceNumber = query[CLOSED_PARAMS.invoiceNumber];

  return (
    <div className="space-y-6">
      <ConsultationHeader view={view} />

      {!open && (
        <ClosedNotice
          justClosed={query[CLOSED_PARAMS.closed] === '1'}
          invoice={invoiceId && invoiceNumber ? { id: invoiceId, number: invoiceNumber } : undefined}
          followUpAt={query[CLOSED_PARAMS.followUpAt]}
        />
      )}

      <div className="grid gap-6 xl:grid-cols-[1fr_26rem]">
        <div className="min-w-0 space-y-6">
          <PartCard id="notes" title={text.notes.title}>
            <NotesForm
              consultationId={consultation.id}
              editable={canWrite}
              initial={{
                chief_complaint: consultation.chief_complaint ?? '',
                subjective: consultation.subjective ?? '',
                objective: consultation.objective ?? '',
                assessment: consultation.assessment ?? '',
                plan: consultation.plan ?? '',
              }}
            />
          </PartCard>

          <PartCard id="examination" title={text.exam.title}>
            <ExaminationForm consultationId={consultation.id} findings={consultation.examination} editable={canWrite} />
          </PartCard>

          <PartCard id="diagnoses" title={text.diagnoses.title}>
            <DiagnosisList diagnoses={consultation.diagnoses} />
            {canWrite && (
              <DiagnosisForm consultationId={consultation.id} hasPrimary={consultation.diagnoses.some((diagnosis) => diagnosis.is_primary)} />
            )}
          </PartCard>

          {/* Lab requests are their own records: results come back after the case is signed, so the list stays when it closes. */}
          {(canOrderLab || view.labRequests !== null) && (
            <PartCard id="lab" title={text.lab.title}>
              <CaseLabRequests requests={view.labRequests} canRecord={context.can(Scope.LabWrite)} />
              {canOrderLab && <LabOrderForm petId={consultation.pet_id} consultationId={consultation.id} />}
            </PartCard>
          )}
        </div>

        <div className="space-y-6">
          <PartCard id="vitals" title={text.vitals.title}>
            <VitalsList vitals={consultation.vitals} />
            {open && context.can(Scope.VitalsWrite) && <VitalsForm consultationId={consultation.id} />}
          </PartCard>

          <PartCard id="treatments" title={text.treatments.title}>
            <TreatmentList treatments={consultation.treatments} totalSatang={consultation.treatment_charges_satang} />
            {canWrite && <TreatmentForm consultationId={consultation.id} />}
          </PartCard>

          <PartCard id="prescriptions" title={text.prescriptions.title}>
            <PrescriptionList prescriptions={consultation.prescriptions} />
            {open && context.can(Scope.PrescriptionWrite) && <PrescriptionForm consultationId={consultation.id} branchId={consultation.branch_id} />}
          </PartCard>

          {open && (
            <PartCard id="close" title={text.close.title}>
              {context.can(Scope.RecordSign) ? (
                <ClosePanel
                  consultationId={consultation.id}
                  services={services}
                  fromAppointment={Boolean(consultation.appointment_id)}
                />
              ) : (
                <p className="text-sm text-ink-600">{text.close.noScope}</p>
              )}
            </PartCard>
          )}
        </div>
      </div>
    </div>
  );
}
