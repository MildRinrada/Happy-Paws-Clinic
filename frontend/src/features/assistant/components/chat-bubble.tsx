import Link from 'next/link';
import type { ChatEntry } from '../types/assistant.types';

/** Models like to write **bold**; the widget shows plain text, so the markers are dropped. */
const plain = (text: string) => text.replace(/\*\*(.+?)\*\*/g, '$1').replace(/^\s*[*-]\s+/gm, '• ');

/** One message: the visitor's on the right, the assistant's on the left with any buttons under it. */
export function ChatBubble({ entry, onNavigate }: { entry: ChatEntry; onNavigate: () => void }) {
  const mine = entry.role === 'user';

  return (
    <li className={`flex flex-col gap-1.5 ${mine ? 'items-end' : 'items-start'}`}>
      <p
        className={`max-w-[85%] whitespace-pre-line rounded-2xl px-3.5 py-2 text-[15px] leading-relaxed ${
          mine ? 'rounded-br-md bg-site-teal text-white' : 'rounded-bl-md bg-site-soft text-ink-900'
        }`}
      >
        {mine ? entry.text : plain(entry.text)}
      </p>

      {entry.actions && entry.actions.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {entry.actions.map((action) => (
            <Link
              key={action.href}
              href={action.href}
              onClick={onNavigate}
              className="rounded-full border border-site-teal px-3 py-1 text-sm font-semibold text-site-teal transition hover:bg-site-teal hover:text-white"
            >
              {action.label}
            </Link>
          ))}
        </div>
      )}
    </li>
  );
}
