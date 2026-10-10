import Link from 'next/link';
import { askPage } from '../../content/ask-page';
import type { AskEntry } from '../../types/staff-ai.types';
import { AiDraftBadge } from '../common/ai-draft-badge';

/** Models like to write **bold**; the page shows plain text, so the markers are dropped. */
const plain = (text: string) => text.replace(/\*\*(.+?)\*\*/g, '$1').replace(/^\s*[*-]\s+/gm, '• ');

/** One message: the person's on the right, the assistant's on the left, labelled and with its buttons. */
export function AskBubble({ entry }: { entry: AskEntry }) {
  const mine = entry.role === 'user';

  return (
    <li className={`flex flex-col gap-1.5 ${mine ? 'items-end' : 'items-start'}`}>
      <span className="flex items-center gap-2 text-xs text-ink-500">
        {mine ? askPage.you : askPage.assistant}
        {!mine && <AiDraftBadge />}
      </span>
      <p
        className={`max-w-[90%] whitespace-pre-line break-words rounded-2xl px-3.5 py-2 text-sm leading-relaxed sm:max-w-[80%] ${
          mine ? 'rounded-br-md bg-brand-600 text-white' : 'rounded-bl-md bg-ink-100 text-ink-900'
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
              className="rounded-full border border-brand-600 px-3 py-1 text-xs font-semibold text-brand-700 hover:bg-brand-50"
            >
              {action.label}
            </Link>
          ))}
        </div>
      )}
    </li>
  );
}
