import { soapDraftText } from '../../content/soap-draft';

/** One drafted field of the form, as it will go in. */
export function DraftField({ name, value }: { name: string; value: string }) {
  return (
    <div>
      <dt className="text-xs font-medium text-ink-600">{soapDraftText.fields[name] ?? name}</dt>
      <dd className={`whitespace-pre-line text-sm ${value ? 'text-ink-900' : 'text-ink-400'}`}>{value || soapDraftText.emptyField}</dd>
    </div>
  );
}
