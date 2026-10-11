import { assignmentRulesText } from '../content/assignment-rules';
import type { AssignmentRules } from '../types/schedule.types';
import { AssignmentRulesForm } from './assignment-rules-form';

interface AssignmentRulesSectionProps {
  people: { id: string; name: string }[];
  services: { id: string; name: string }[];
  /** The stored rules (docs/contract-extensions.md § 48), or `null` when they could not be read. */
  stored: AssignmentRules | null;
}

/**
 * The case-assignment rules, folded away under the roster: set rarely, and by the person who runs
 * the rota. Says up front how the rules act, then edits the set as it is stored.
 */
export function AssignmentRulesSection({ people, services, stored }: AssignmentRulesSectionProps) {
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
        <AssignmentRulesForm people={people} services={services} stored={stored} />
      </div>
    </details>
  );
}
