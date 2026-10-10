import { Badge } from '@/components/ui/badge';
import { policyStatusLabels } from '../../content/staff-privacy-policy';
import type { StaffPolicyVersion } from '../../types/staff-privacy-policy.types';

/** Draft, published, or the current notice, in the clinic's words. */
export function PolicyStatusBadge({ version }: { version: StaffPolicyVersion }) {
  const label = policyStatusLabels[version.is_current ? 'current' : version.status];

  return <Badge tone={label.tone}>{label.text}</Badge>;
}
