interface PutOnlyIntroProps {
  how: string[];
  /** Says plainly that the stored values cannot be shown and where the form starts instead. */
  cannotRead: string;
}

/** How a write-only setting acts, and that the form does not show what is stored now. */
export function PutOnlyIntro({ how, cannotRead }: PutOnlyIntroProps) {
  return (
    <div className="space-y-3">
      <ul className="list-disc space-y-1 pl-5 text-sm text-ink-700">
        {how.map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ul>
      <p role="note" className="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
        {cannotRead}
      </p>
    </div>
  );
}
