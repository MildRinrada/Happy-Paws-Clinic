import { labPage } from '../content/lab-page';

/** What the vet wrote when reading the results (§ 53), wherever the results are shown. */
export function LabReviewNote({ note }: { note?: string }) {
  if (!note) return null;

  return (
    <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm">
      <p className="text-xs font-medium text-emerald-800">{labPage.reviewNote.label}</p>
      <p className="mt-0.5 whitespace-pre-line text-ink-800">{note}</p>
    </div>
  );
}
