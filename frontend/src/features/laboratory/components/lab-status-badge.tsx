import { Badge, type BadgeTone } from '@/components/ui/badge';
import { labPage } from '../content/lab-page';
import type { LabStatus } from '../types/lab.types';

const tones: Record<LabStatus, BadgeTone> = { pending: 'amber', resulted: 'blue', reviewed: 'green' };

/** Where a request is: waiting on the lab, waiting on a vet, or read. */
export function LabStatusBadge({ status }: { status: LabStatus }) {
  return <Badge tone={tones[status]}>{labPage.request.status[status]}</Badge>;
}
