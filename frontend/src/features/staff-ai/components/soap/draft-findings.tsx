import { soapDraftText } from '../../content/soap-draft';
import type { DraftFinding } from '../../types/staff-ai.types';

/** The drafted examination lines: only the body systems the notes mention. */
export function DraftFindings({ findings }: { findings: DraftFinding[] }) {
  if (findings.length === 0) return null;

  const text = soapDraftText;

  return (
    <div>
      <p className="text-xs font-medium text-ink-600">{text.fields.examination}</p>
      <ul className="mt-1 space-y-1 text-sm">
        {findings.map((finding) => (
          <li key={finding.body_system} className="flex flex-wrap gap-x-2">
            <span className="font-medium text-ink-900">{finding.body_system}</span>
            <span className={finding.is_normal ? 'text-emerald-700' : 'text-red-700'}>
              {finding.is_normal ? text.normal : text.abnormal}
            </span>
            {finding.note && <span className="text-ink-700">{finding.note}</span>}
          </li>
        ))}
      </ul>
    </div>
  );
}
