import { Badge } from '@/components/ui/badge';
import { packageStatusLabels } from '../../content/crm-labels';
import type { PackageStatus } from '../../types/crm.types';

/** Where a package stands: waiting for its bill, usable, or finished one way or another. */
export function PackageStatusBadge({ status }: { status: PackageStatus }) {
  const label = packageStatusLabels[status];

  return <Badge tone={label.tone}>{label.text}</Badge>;
}
