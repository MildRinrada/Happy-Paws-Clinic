import { Badge } from '@/components/ui/badge';
import { formatDateTime } from '@/lib/format/datetime';
import { labPage } from '../content/lab-page';
import type { LabRequest } from '../types/lab.types';
import { LabStatusBadge } from './lab-status-badge';

/** What was asked for, where it went, and when: the top of a request wherever it is shown. */
export function LabRequestSummary({ request }: { request: LabRequest }) {
  const text = labPage.request;
  const times = [
    text.requestedAt(formatDateTime(request.requested_at)),
    request.resulted_at && text.resultedAt(formatDateTime(request.resulted_at)),
    request.reviewed_at && text.reviewedAt(formatDateTime(request.reviewed_at)),
  ].filter(Boolean);

  return (
    <div className="space-y-1.5 text-sm">
      <div className="flex flex-wrap items-center gap-2">
        <LabStatusBadge status={request.status} />
        {request.priority === 'urgent' && <Badge tone="red">{text.urgent}</Badge>}
        {request.abnormal_count > 0 && <Badge tone="red">{text.abnormal(request.abnormal_count)}</Badge>}
        <p className="font-medium text-ink-900">
          {request.tests.map((test) => (test.code ? `${test.name} (${test.code})` : test.name)).join(', ')}
        </p>
      </div>
      <p className="text-ink-600">
        {request.destination === 'external' ? text.external(request.external_lab_name ?? '') : text.inHouse}
        {' / '}
        {times.join(' / ')}
      </p>
      {request.clinical_notes && (
        <p className="text-ink-600">
          <span className="text-ink-500">{text.notes}</span> {request.clinical_notes}
        </p>
      )}
    </div>
  );
}
