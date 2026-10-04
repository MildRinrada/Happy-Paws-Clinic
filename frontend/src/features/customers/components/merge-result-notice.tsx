import Link from 'next/link';
import { mergeCustomer } from '../content/merge-customer';
import { customerPaths } from '../lib/customer-paths';
import type { MergeResult } from '../types/customer.types';

/** What the merge moved, table by table, as the backend counted it. Tables with nothing are left out. */
export function MergeResultNotice({ result }: { result: MergeResult }) {
  const text = mergeCustomer.done;
  const moved = Object.entries(result.moved).filter(([, count]) => count > 0);

  return (
    <div role="status" className="space-y-2 rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2.5 text-sm text-emerald-900">
      <p className="font-medium">{text.title(result.merged.display_name)}</p>
      {moved.length === 0 ? (
        <p>{text.nothing}</p>
      ) : (
        <ul className="list-disc space-y-0.5 pl-5">
          {moved.map(([key, count]) => (
            <li key={key}>{text.count(text.labels[key] ?? key, count)}</li>
          ))}
        </ul>
      )}
      <Link href={customerPaths.detail(result.merged.id)} className="inline-block font-medium underline">
        {text.openMerged}
      </Link>
    </div>
  );
}
