import { sendPage } from '../content/send-page';
import type { Recipient } from '../types/notification.types';

interface ChosenRecipientsProps {
  recipients: Recipient[];
  onRemove: (id: string) => void;
}

/** The customers picked so far, each removable. */
export function ChosenRecipients({ recipients, onRemove }: ChosenRecipientsProps) {
  if (recipients.length === 0) {
    return <p className="text-sm text-ink-500">{sendPage.noRecipients}</p>;
  }

  return (
    <ul className="flex flex-wrap gap-2">
      {recipients.map((recipient) => (
        <li key={recipient.id} className="inline-flex items-center gap-1 rounded-full bg-brand-50 py-1 pl-3 pr-1 text-sm text-brand-800 ring-1 ring-brand-200">
          {recipient.name}
          <button
            type="button"
            aria-label={sendPage.remove(recipient.name)}
            className="rounded-full px-1.5 text-brand-700 hover:bg-brand-100"
            onClick={() => onRemove(recipient.id)}
          >
            ×
          </button>
        </li>
      ))}
    </ul>
  );
}
