import { labPage } from '../../content/lab-page';
import type { LabWait } from '../../lib/waiting-time';

/** "รอ 25 นาที", in red with "เกินเวลา" once a sample has waited on the lab past the clinic's limit. */
export function LabWaitingTime({ wait }: { wait: LabWait }) {
  if (!wait.overdue) return <span className="whitespace-nowrap tabular-nums text-ink-600">{wait.text}</span>;

  return (
    <span className="inline-flex items-center gap-1.5 whitespace-nowrap font-medium tabular-nums text-red-700">
      <span aria-hidden className="size-1.5 rounded-full bg-red-600" />
      {wait.text}
      <span className="rounded bg-red-100 px-1.5 text-xs text-red-800">{labPage.row.overdue}</span>
    </span>
  );
}
