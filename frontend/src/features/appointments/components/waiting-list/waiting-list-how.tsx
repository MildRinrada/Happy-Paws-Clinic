import { waitingListText } from '../../content/advanced-booking';

/** How the waiting list acts, and that entries cannot be listed or withdrawn once added. */
export function WaitingListHow() {
  const text = waitingListText;

  return (
    <div className="space-y-3">
      <ul className="list-disc space-y-1 pl-5 text-sm text-ink-700">
        {text.how.map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ul>
      <p role="note" className="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
        {text.cannotList}
      </p>
    </div>
  );
}
