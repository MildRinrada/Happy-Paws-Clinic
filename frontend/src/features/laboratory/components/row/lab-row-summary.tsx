import { Badge } from '@/components/ui/badge';
import { labPage } from '../../content/lab-page';
import type { LabRow } from '../../lib/load-lab-worklist';
import type { LabWait } from '../../lib/waiting-time';
import { LabStatusBadge } from '../lab-status-badge';
import { LabWaitingTime } from './lab-waiting-time';

/** A test as the row names it: the name, and the code when it says something the name does not. */
const testLabel = (test: { name: string; code?: string }) => (test.code && test.code !== test.name ? `${test.name} (${test.code})` : test.name);

/**
 * The one line of a closed row: who, what, where it went, how long it has waited, and where it is.
 * Spans only, because it sits inside the row's button. Three short lines on a phone, one on a desk.
 */
export function LabRowSummary({ row, wait }: { row: LabRow; wait?: LabWait }) {
  const { request, pet } = row;
  const text = labPage.request;

  return (
    <span className="flex min-w-0 flex-1 flex-col gap-1 sm:flex-row sm:items-center sm:gap-4">
      <span className="flex min-w-0 items-baseline gap-2 sm:w-52 sm:shrink-0">
        <span className="truncate font-medium text-ink-900">{pet ? pet.name : request.pet_id}</span>
        {pet && <span className="shrink-0 text-sm text-ink-500">{pet.species}</span>}
        {pet && <span className="hidden shrink-0 font-mono text-xs text-ink-500 lg:inline">{pet.hospitalNumber}</span>}
      </span>
      <span className="flex min-w-0 items-center gap-2 sm:flex-1">
        {request.priority === 'urgent' && <Badge tone="red">{text.urgent}</Badge>}
        <span className="truncate text-sm text-ink-800">{request.tests.map(testLabel).join(', ')}</span>
      </span>
      <span className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm sm:shrink-0 sm:justify-end">
        <span className="max-w-40 truncate text-ink-600">
          {request.destination === 'external' ? request.external_lab_name ?? text.external('') : text.inHouseShort}
        </span>
        {wait && <LabWaitingTime wait={wait} />}
        {request.abnormal_count > 0 && <Badge tone="red">{text.abnormal(request.abnormal_count)}</Badge>}
        <LabStatusBadge status={request.status} />
      </span>
    </span>
  );
}
