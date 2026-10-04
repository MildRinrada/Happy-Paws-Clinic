import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { LabOwnerToggle } from '@/features/laboratory/components/lab-owner-toggle';
import { LabStatusTabs } from '@/features/laboratory/components/lab-status-tabs';
import { LabWorklist } from '@/features/laboratory/components/lab-worklist';
import { labPage } from '@/features/laboratory/content/lab-page';
import { parseLabFilters } from '@/features/laboratory/lib/lab-filters';
import { loadLabWorklist, type LabWorklistView } from '@/features/laboratory/lib/load-lab-worklist';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: labPage.metaTitle };

/**
 * The lab worklist at the branch in view: what waits on the lab, and what waits on a vet. The lab
 * enters results here (`lab:write`); a vet marks them read (`lab:read`). Ordering a test happens
 * in the consulting room, where the patient and the case are.
 */
export default async function LabPage({ searchParams }: { searchParams: Promise<Record<string, string | undefined>> }) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.LabRead)) {
    return <Alert tone="info">{labPage.noAccess}</Alert>;
  }
  if (!context.branch) {
    return <Alert tone="info">{labPage.noBranch}</Alert>;
  }

  const branch = context.branch;
  const canRecord = context.can(Scope.LabWrite);
  const filters = parseLabFilters(params, canRecord);

  let view: LabWorklistView;
  try {
    view = await authed(() => loadLabWorklist(filters, branch.id));
  } catch (error) {
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  return (
    <div className="max-w-5xl space-y-6">
      <PageHeader title={labPage.title} subtitle={labPage.subtitle(branch.name)} />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <LabStatusTabs filters={filters} />
        <LabOwnerToggle filters={filters} />
      </div>

      <LabWorklist
        view={view}
        filters={filters}
        canRecord={canRecord}
        canReview={context.can(Scope.LabRead)}
        canOpenCase={context.can(Scope.ConsultRead)}
      />
    </div>
  );
}
