import { waitingListText } from '../../content/advanced-booking';

/** How the waiting list acts: who is told, when, and how an entry leaves the list. */
export function WaitingListHow() {
  return (
    <ul className="list-disc space-y-1 pl-5 text-sm text-ink-700">
      {waitingListText.how.map((line) => (
        <li key={line}>{line}</li>
      ))}
    </ul>
  );
}
