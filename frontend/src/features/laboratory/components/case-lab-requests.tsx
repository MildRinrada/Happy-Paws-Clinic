import Link from 'next/link';
import { labOrder } from '../content/lab-order';
import { LAB_PATH } from '../lib/lab-filters';
import type { LabRequest } from '../types/lab.types';
import { LabRequestActions } from './lab-request-actions';
import { LabRequestSummary } from './lab-request-summary';
import { LabResultTable } from './lab-result-table';

interface CaseLabRequestsProps {
  /** `null` when the session may not read lab requests (`lab:read`). */
  requests: LabRequest[] | null;
  canRecord: boolean;
}

/** The lab requests sent from this case, newest first, with their results as they come in. */
export function CaseLabRequests({ requests, canRecord }: CaseLabRequestsProps) {
  const text = labOrder;

  if (requests === null) return <p className="text-sm text-ink-500">{text.cannotRead}</p>;
  if (requests.length === 0) return <p className="text-sm text-ink-500">{text.none}</p>;

  return (
    <div className="space-y-3">
      <ul className="divide-y divide-ink-100">
        {requests.map((request) => (
          <li key={request.id} className="space-y-2 py-3 first:pt-0">
            <LabRequestSummary request={request} />
            {request.results.length > 0 && <LabResultTable results={request.results} />}
            <LabRequestActions request={request} canRecord={canRecord} canReview />
          </li>
        ))}
      </ul>
      <Link href={LAB_PATH} className="text-sm font-medium text-brand-700 hover:underline">
        {text.worklist}
      </Link>
    </div>
  );
}
