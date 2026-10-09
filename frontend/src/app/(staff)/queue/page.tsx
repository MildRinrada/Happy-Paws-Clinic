import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { EmptyState } from '@/components/ui/empty-state';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { IssueTicketDialog } from '@/features/queue/components/issue-ticket-dialog';
import { QueueBoard } from '@/features/queue/components/queue-board';
import { QueueLiveIndicator } from '@/features/queue/components/queue-live-indicator';
import { QueueNavLinks } from '@/features/queue/components/queue-nav-links';
import { loadQueueView } from '@/features/queue/lib/load-queue-view';
import type { QueueView } from '@/features/queue/types/queue.types';
import { listServices } from '@/features/service-catalog/api/listServices';
import { ApiError, optional } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { formatLongDate } from '@/lib/format/datetime';

export const metadata: Metadata = { title: 'คิววันนี้ · Happy Paws Clinic' };

/**
 * Today's queue at the branch in view.
 *
 * A server component: it reads the board on every render and holds no copy of it. The live
 * indicator re-runs it whenever the stream says something moved, so what is on screen is always a
 * read of the backend and never a guess assembled from events.
 */
export default async function QueuePage() {
  const context = await loadStaffContext();

  if (!context?.branch) {
    return <EmptyState title="ยังไม่มีสาขาที่บัญชีนี้เข้าถึงได้">ติดต่อผู้ดูแลระบบเพื่อกำหนดสาขา</EmptyState>;
  }

  const { branch } = context;

  if (!context.can(Scope.QueueRead)) {
    return <Alert tone="info">บัญชีนี้ไม่มีสิทธิ์ดูคิว (queue:read)</Alert>;
  }

  let view: QueueView;

  try {
    view = await authed(() => loadQueueView(branch.id));
  } catch (error) {
    // A refusal is an answer, shown in the backend's own words. Anything else is a fault and is
    // left to the error boundary.
    if (error instanceof ApiError && error.status === 403) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  const canOrganise = context.can(Scope.QueueWrite);
  const services = canOrganise ? ((await optional(listServices(branch.id))) ?? []) : [];

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-display text-2xl font-semibold text-ink-900">คิววันนี้</h1>
          <p className="text-sm text-ink-600">
            {branch.name} · {formatLongDate(view.board.service_date)} · รอเรียก {view.board.waiting_count} คิว
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <QueueLiveIndicator branchId={branch.id} />
          <QueueNavLinks branchId={branch.id} />
          {canOrganise && <IssueTicketDialog branchId={branch.id} services={services} />}
        </div>
      </header>

      <QueueBoard
        view={view}
        rooms={branch.exam_rooms}
        canCall={context.can(Scope.QueueCall)}
        canOrganise={canOrganise}
        canConsult={context.can(Scope.ConsultWrite)}
      />
    </div>
  );
}
