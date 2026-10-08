import { Badge } from '@/components/ui/badge';
import { drugPicker } from '../../content/drug-picker';

/**
 * Whether a written drug is tied to a stock item (docs/contract-extensions.md § 20). A tied drug is
 * taken off the shelf and charged when dispensed; an untied one only if its name matches an item exactly.
 */
export function StockLinkBadge({ linked }: { linked: boolean }) {
  return linked ? <Badge tone="blue">{drugPicker.list.linked}</Badge> : <Badge tone="neutral">{drugPicker.list.unlinked}</Badge>;
}
