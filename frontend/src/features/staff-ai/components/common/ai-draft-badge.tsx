import { Badge } from '@/components/ui/badge';
import { aiCommon } from '../../content/ai-common';

/** The label on everything the model wrote: a draft, not a record. */
export function AiDraftBadge() {
  return <Badge tone="amber">{aiCommon.draftLabel}</Badge>;
}
