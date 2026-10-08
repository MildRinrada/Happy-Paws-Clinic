import { Badge } from '@/components/ui/badge';
import { automationsPage } from '../content/automations-page';

/** Whether the scheduler runs this rule. */
export function AutomationStatusBadge({ enabled }: { enabled: boolean }) {
  return <Badge tone={enabled ? 'green' : 'neutral'}>{enabled ? automationsPage.enabled : automationsPage.disabled}</Badge>;
}
