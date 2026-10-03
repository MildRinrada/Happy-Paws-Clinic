import { assistantWidget } from '../content/assistant-widget';

/** Three pulsing dots while the reply is on its way. */
export function TypingDots() {
  return (
    <li className="flex" aria-live="polite">
      <span className="sr-only">{assistantWidget.typing}</span>
      <span aria-hidden="true" className="flex gap-1 rounded-2xl rounded-bl-md bg-site-soft px-3.5 py-3">
        {[0, 150, 300].map((delay) => (
          <span key={delay} className="size-2 animate-pulse rounded-full bg-site-muted" style={{ animationDelay: `${delay}ms` }} />
        ))}
      </span>
    </li>
  );
}
