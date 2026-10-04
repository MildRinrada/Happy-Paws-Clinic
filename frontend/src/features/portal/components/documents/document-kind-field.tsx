import { petDocuments } from '../../content/pet-documents';
import { DOCUMENT_KINDS, type DocumentKind } from '../../lib/document-rules';

interface DocumentKindFieldProps {
  value: DocumentKind;
  onChange: (kind: DocumentKind) => void;
  disabled?: boolean;
}

/** What the file is, as a row of choices, so the clinic files it under the right heading. */
export function DocumentKindField({ value, onChange, disabled }: DocumentKindFieldProps) {
  return (
    <fieldset className="space-y-1.5" disabled={disabled}>
      <legend className="text-sm font-semibold text-ink-900">{petDocuments.kind}</legend>
      <div className="flex flex-wrap gap-2 pt-1">
        {DOCUMENT_KINDS.map((kind) => (
          <label
            key={kind}
            className="cursor-pointer rounded-full border border-ink-200 bg-white px-3 py-1.5 text-sm text-ink-700 has-[:checked]:border-site-teal has-[:checked]:bg-brand-50 has-[:checked]:font-semibold has-[:checked]:text-site-teal has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-site-teal"
          >
            <input
              type="radio"
              name="document-kind"
              value={kind}
              checked={value === kind}
              className="sr-only"
              onChange={() => onChange(kind)}
            />
            {petDocuments.kinds[kind]}
          </label>
        ))}
      </div>
    </fieldset>
  );
}
