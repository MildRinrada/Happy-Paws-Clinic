import { assignmentRulesText } from '../content/assignment-rules';
import { AssignmentRulesForm } from './assignment-rules-form';

interface AssignmentRulesSectionProps {
  people: { id: string; name: string }[];
  services: { id: string; name: string }[];
}

/**
 * The case-assignment rules, folded away under the roster: set rarely, and by the person who runs
 * the rota. Says up front how the rules act and that the current ones cannot be shown.
 */
export function AssignmentRulesSection({ people, services }: AssignmentRulesSectionProps) {
  const text = assignmentRulesText;

  return (
    <details className="group rounded-xl border border-ink-200 bg-white">
      <summary className="flex cursor-pointer list-none items-center justify-between gap-4 px-5 py-3.5 [&::-webkit-details-marker]:hidden">
        <span>
          <span className="block font-display text-base font-semibold text-ink-900">{text.heading}</span>
          <span className="block text-sm text-ink-500">{text.summary}</span>
        </span>
        <span aria-hidden="true" className="text-ink-400 transition-transform group-open:rotate-180">
          ▾
        </span>
      </summary>

      <div className="space-y-4 border-t border-ink-100 px-5 py-4">
        <ul className="list-disc space-y-1 pl-5 text-sm text-ink-700">
          {text.how.map((line) => (
            <li key={line}>{line}</li>
          ))}
        </ul>
        <p role="note" className="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
          {text.cannotRead}
        </p>
        <AssignmentRulesForm people={people} services={services} />
      </div>
    </details>
  );
}
