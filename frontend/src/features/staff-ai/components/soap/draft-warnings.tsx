import { soapDraftText } from '../../content/soap-draft';
import type { DraftWarning } from '../../types/staff-ai.types';

/**
 * What the draft says that the notes do not: a dose, a number or a term. A prompt for a second
 * look, not a verdict; the check cannot read Thai drug names or tell a translation from an addition.
 */
export function DraftWarnings({ warnings }: { warnings: DraftWarning[] }) {
  if (warnings.length === 0) return null;

  const text = soapDraftText;

  return (
    <div role="note" className="space-y-1.5 rounded-lg border border-red-200 bg-red-50 px-3 py-2">
      <p className="text-sm font-semibold text-red-800">{text.warningsTitle}</p>
      <ul className="flex flex-wrap gap-1.5">
        {warnings.map((warning) => (
          <li key={`${warning.field}-${warning.text}`} className="rounded-full bg-white px-2 py-0.5 text-xs text-red-800">
            {text.warningReasons[warning.reason] ?? warning.reason} {warning.text}
            <span className="text-red-600"> ({text.fields[warning.field] ?? warning.field})</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
