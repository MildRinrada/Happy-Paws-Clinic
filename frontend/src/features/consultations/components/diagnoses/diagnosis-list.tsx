import { Badge } from '@/components/ui/badge';
import { consultationPage } from '../../content/consultation-page';
import type { Diagnosis } from '../../types/consultation.types';

/** The diagnoses so far, the primary one first as the API orders them. */
export function DiagnosisList({ diagnoses }: { diagnoses: Diagnosis[] }) {
  const text = consultationPage.diagnoses;

  if (diagnoses.length === 0) return <p className="text-sm text-amber-800">{text.none}</p>;

  return (
    <ul className="space-y-2 text-sm">
      {diagnoses.map((diagnosis) => (
        <li key={diagnosis.id} className="flex flex-wrap items-start gap-2">
          {diagnosis.is_primary && <Badge tone="brand">{text.primary}</Badge>}
          <div className="min-w-0">
            <p className="font-medium text-ink-900">
              {diagnosis.description}
              {diagnosis.code && <span className="ml-2 font-mono text-xs text-ink-500">{diagnosis.code}</span>}
            </p>
            {diagnosis.note && <p className="text-ink-600">{diagnosis.note}</p>}
          </div>
        </li>
      ))}
    </ul>
  );
}
