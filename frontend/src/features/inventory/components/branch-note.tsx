import { inventoryArea } from '../content/inventory-tabs';

/** Which branch's stock the page is showing. Stock is per branch; the console's branch picker chooses it. */
export function BranchNote({ name }: { name: string }) {
  return <span className="text-sm text-ink-600">{inventoryArea.branchNote(name)}</span>;
}
