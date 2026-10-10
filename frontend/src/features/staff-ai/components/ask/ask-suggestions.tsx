import { askPage } from '../../content/ask-page';

/** Starting questions, shown until the first one is asked. */
export function AskSuggestions({ onPick, disabled }: { onPick: (question: string) => void; disabled: boolean }) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {askPage.suggestions.map((suggestion) => (
        <button
          key={suggestion}
          type="button"
          disabled={disabled}
          onClick={() => onPick(suggestion)}
          className="rounded-full bg-ink-100 px-3 py-1.5 text-sm text-ink-800 hover:bg-ink-200 disabled:opacity-50"
        >
          {suggestion}
        </button>
      ))}
    </div>
  );
}
