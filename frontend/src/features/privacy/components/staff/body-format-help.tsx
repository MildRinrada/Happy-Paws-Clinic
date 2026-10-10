import { formatHelp } from '../../content/staff-privacy-policy';

/** The three rules of the body format, beside the editor. */
export function BodyFormatHelp() {
  return (
    <details className="rounded-lg border border-ink-200 bg-ink-50 px-4 py-3 text-sm text-ink-700">
      <summary className="cursor-pointer font-medium text-ink-800">{formatHelp.title}</summary>
      <ul className="mt-2 list-disc space-y-1 break-words pl-5">
        {formatHelp.rules.map((rule) => (
          <li key={rule}>{rule}</li>
        ))}
      </ul>
    </details>
  );
}
