'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replaceAssignmentRulesAction } from '../actions/schedule-actions';
import { assignmentRulesText } from '../content/assignment-rules';
import {
  MAX_RULES,
  draftsFrom,
  emptyRule,
  moved,
  ruleProblem,
  startingRules,
  toRequest,
  type RuleDraft,
} from '../lib/assignment-rule-draft';
import type { AssignmentRules } from '../types/schedule.types';
import { AssignmentRuleRow } from './assignment-rule-row';

interface AssignmentRulesFormProps {
  people: { id: string; name: string }[];
  services: { id: string; name: string }[];
  /** The stored set and its version, or `null` when it could not be read. */
  stored: AssignmentRules | null;
}

const SPECIES_LIST_ID = 'assignment-rule-species';

/**
 * The whole rule set, written in one go. It starts from the stored rules
 * (docs/contract-extensions.md § 48), or from the contract's example when there are none yet, and
 * saves with the version it was read at, so a save over someone else's answers a conflict instead
 * of quietly replacing their rules.
 */
export function AssignmentRulesForm({ people, services, stored: initial }: AssignmentRulesFormProps) {
  const text = assignmentRulesText;
  const [rules, setRules] = useState<RuleDraft[]>(() =>
    initial && initial.rules.length > 0 ? draftsFrom(initial.rules) : startingRules(),
  );
  const [checked, setChecked] = useState(false);
  const [understood, setUnderstood] = useState(false);
  const [stored, setStored] = useState<AssignmentRules | undefined>(initial ?? undefined);
  const [answered, setAnswered] = useState(false);
  const showsSample = !answered && (!initial || initial.rules.length === 0);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);
  const problems = rules.map(ruleProblem);
  // Refusals here name a field (`staff_ids`, `service_id`) but not which rule; one line says it.
  const refusal = message ?? Object.values(errors)[0];

  function change(next: RuleDraft[]) {
    setRules(next);
    touch();
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setChecked(true);
    if (problems.some(Boolean)) return;

    submit(
      () => replaceAssignmentRulesAction({ rules: rules.map(toRequest), version: stored?.version }),
      (answer) => {
        setStored(answer);
        setAnswered(true);
        setRules(draftsFrom(answer.rules));
        setChecked(false);
        setUnderstood(false);
      },
    );
  }

  return (
    <form onSubmit={save} className="space-y-4">
      {showsSample && (
        <p role="note" className="rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
          {initial ? text.sample : text.readFailed}
        </p>
      )}
      {stored && saved && (
        <p role="status" className="rounded-lg border border-emerald-200 bg-emerald-50 px-3.5 py-2.5 text-sm text-emerald-900">
          {stored.rules.length > 0 ? text.savedSet(stored.rules.length) : text.savedNone}
        </p>
      )}

      <datalist id={SPECIES_LIST_ID}>
        {text.speciesOptions.map((species) => (
          <option key={species} value={species} />
        ))}
      </datalist>

      {rules.length === 0 ? (
        <p className="rounded-lg border border-dashed border-ink-200 px-4 py-6 text-center text-sm text-ink-500">{text.empty}</p>
      ) : (
        <ol className="space-y-3">
          {rules.map((rule, index) => (
            <AssignmentRuleRow
              key={rule.key}
              rule={rule}
              index={index}
              count={rules.length}
              people={people}
              services={services}
              speciesListId={SPECIES_LIST_ID}
              problem={checked ? problems[index] : undefined}
              disabled={pending}
              onChange={(next) => change(rules.map((other, at) => (at === index ? next : other)))}
              onMove={(step) => change(moved(rules, index, index + step))}
              onRemove={() => change(rules.filter((_, at) => at !== index))}
            />
          ))}
        </ol>
      )}

      {rules.length < MAX_RULES ? (
        <Button
          variant="secondary"
          size="sm"
          disabled={pending}
          onClick={() => {
            // A rule added a moment ago is not wrong yet; the next save checks every rule again.
            setChecked(false);
            change([...rules, emptyRule()]);
          }}
        >
          {text.addRule}
        </Button>
      ) : (
        <p className="text-sm text-ink-500">{text.maxRules}</p>
      )}

      <div className="space-y-3 border-t border-ink-100 pt-4">
        <Checkbox label={text.acknowledge} checked={understood} onChange={(event) => setUnderstood(event.target.checked)} />
        <div className="flex flex-wrap items-center justify-end gap-3">
          <FormStatus message={checked && problems.some(Boolean) ? text.fixRules : refusal} savedText="" />
          <Button type="submit" variant={rules.length === 0 ? 'danger' : 'primary'} disabled={!understood || pending}>
            {pending ? text.submitting : rules.length === 0 ? text.submitClear : text.submit}
          </Button>
        </div>
      </div>
    </form>
  );
}
