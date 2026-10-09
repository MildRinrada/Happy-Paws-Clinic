import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { HistoryFilterForm } from '@/features/queue/components/history-filter-form';
import { HistoryTable } from '@/features/queue/components/history-table';
import { queueHistory } from '@/features/queue/content/queue-history';
import { matchesFilter, readHistoryQuery } from '@/features/queue/lib/history-filters';
import { loadQueueView } from '@/features/queue/lib/load-queue-view';
import type { QueueView } from '@/features/queue/types/queue.types';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { clinicDay, formatLongDate } from '@/lib/format/datetime';

export const metadata: Metadata = { title: queueHistory.metaTitle };

interface Props {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/**
 * One day's queue looked back on: finished tickets by default, any status or any day by choice.
 *
 * Built on the same `GET /staff/queue` as the board, with `date` and `include_finished=true`.
 * The backend has no status filter, so the status choice narrows the day here.
 */
export default async function QueueHistoryPage({ searchParams }: Props) {
  const context = await loadStaffContext();

  if (!context?.branch) {
    return <EmptyState title={queueHistory.noBranch} />;
  }

  if (!context.can(Scope.QueueRead)) {
    return <Alert tone="info">{queueHistory.noAccess}</Alert>;
  }

  const { branch } = context;
  const query = readHistoryQuery(await searchParams);

  let view: QueueView;

  try {
    view = await authed(() => loadQueueView(branch.id, { date: query.date, includeFinished: true }));
  } catch (error) {
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  const tickets = view.tickets.filter((item) => matchesFilter(item.ticket.status, query.filter));

  return (
    <div className="space-y-5">
      <BackLink href="/queue">{queueHistory.back}</BackLink>

      <PageHeader
        title={queueHistory.title}
        subtitle={queueHistory.subtitle(branch.name, formatLongDate(view.board.service_date), tickets.length)}
      />

      <HistoryFilterForm query={query} today={clinicDay(new Date())} />

      <HistoryTable tickets={tickets} />

      <p className="text-xs text-ink-500">{queueHistory.limitations}</p>
    </div>
  );
}
