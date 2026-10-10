import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listQueueDisplays } from '@/features/queue/api/queueDisplays';
import { DisplayCreateForm } from '@/features/queue/components/displays/display-create-form';
import { DisplayList } from '@/features/queue/components/displays/display-list';
import { queueDisplays } from '@/features/queue/content/queue-displays';
import type { QueueDisplaySummary } from '@/features/queue/types/queue.types';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: queueDisplays.metaTitle };

/**
 * The waiting-room screens of the branch in view: make one, see which are working, stop one
 * (docs/contract-extensions.md § 30). `queue:write`, the scope that already organises the board.
 */
export default async function QueueDisplaysPage() {
  const context = await loadStaffContext();

  if (!context?.branch) {
    return <EmptyState title={queueDisplays.noBranch} />;
  }

  if (!context.can(Scope.QueueWrite)) {
    return <Alert tone="info">{queueDisplays.noAccess}</Alert>;
  }

  const { branch } = context;
  let displays: QueueDisplaySummary[];

  try {
    displays = await authed(() => listQueueDisplays(branch.id));
  } catch (error) {
    if (error instanceof ApiError && error.status === 403) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  return (
    <div className="space-y-5">
      <BackLink href="/queue">{queueDisplays.back}</BackLink>

      <PageHeader title={queueDisplays.title} subtitle={queueDisplays.subtitle(branch.name)} />

      <p className="max-w-3xl text-sm text-ink-600">{queueDisplays.intro}</p>

      <DisplayCreateForm branchId={branch.id} />

      <DisplayList displays={displays} />
    </div>
  );
}
