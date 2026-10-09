'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { errorsUnder } from '@/features/inventory/lib/errors-under';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createSegmentAction } from '../../actions/crm-actions';
import { tierLabels } from '../../content/crm-labels';
import { crmArea } from '../../content/crm-tabs';
import { segmentPage } from '../../content/segment-page';
import { emptySegmentDraft, segmentProblems, toSegmentInput, type SegmentDraft, type SegmentProblem } from '../../lib/segment-draft';
import type { BranchOption, LoyaltyTier, Segment } from '../../types/crm.types';
import { BranchSelect } from '../shared/branch-select';
import { SegmentedChoice } from '../shared/segmented-choice';
import { DayCountField } from './day-count-field';
import { SegmentResult } from './segment-result';
import { SpeciesField } from './species-field';

interface Props {
  branches: BranchOption[];
  canBroadcast: boolean;
}

/** `POST /staff/segments`: a name and the conditions. The answer says how many customers match. */
export function SegmentForm({ branches, canBroadcast }: Props) {
  const text = segmentPage;
  const [draft, setDraft] = useState<SegmentDraft>(emptySegmentDraft);
  const [tried, setTried] = useState(false);
  const [segment, setSegment] = useState<Segment>();
  const { errors, message, pending, submit, touch } = useFormSubmit({ conflict: crmArea.conflict, unreachable: crmArea.unreachable });
  const problems = segmentProblems(draft);
  const show = (problem: SegmentProblem) => (tried && problems.includes(problem) ? text.errors[problem] : undefined);
  const server = errorsUnder(errors, 'criteria.');

  function change(patch: Partial<SegmentDraft>) {
    touch();
    setDraft((current) => ({ ...current, ...patch }));
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setTried(true);
    if (problems.length > 0) return;

    submit(() => createSegmentAction(toSegmentInput(draft)), setSegment);
  }

  function another() {
    setSegment(undefined);
    setDraft(emptySegmentDraft());
    setTried(false);
  }

  if (segment) {
    return <SegmentResult segment={segment} canBroadcast={canBroadcast} onAnother={another} />;
  }

  return (
    <form onSubmit={save} className="space-y-5" noValidate>
      <Field label={text.name} htmlFor="segment-name" error={errors.name ?? show('name')}>
        <Input
          id="segment-name"
          value={draft.name}
          maxLength={120}
          placeholder={text.namePlaceholder}
          onChange={(event) => change({ name: event.target.value })}
        />
      </Field>

      <section className="space-y-4 rounded-lg border border-ink-200 p-4">
        <div>
          <h2 className="text-sm font-medium text-ink-800">{text.criteriaHeading}</h2>
          <p className="text-sm text-ink-500">{text.criteriaHint}</p>
          {(show('noCriteria') ?? errors.criteria) && (
            <p role="alert" className="mt-1 text-sm text-red-700">
              {show('noCriteria') ?? errors.criteria}
            </p>
          )}
        </div>

        <SpeciesField value={draft.species} onChange={(species) => change({ species })} />

        <div className="grid gap-4 sm:grid-cols-2">
          <DayCountField
            id="segment-inactive"
            label={text.inactiveDays}
            hint={text.inactiveHint}
            error={server.inactive_days ?? show('inactiveDays')}
            value={draft.inactiveDays}
            quickDays={text.quickDays}
            onChange={(inactiveDays) => change({ inactiveDays })}
          />
          <DayCountField
            id="segment-within"
            label={text.visitedWithinDays}
            hint={text.visitedWithinHint}
            error={server.visited_within_days ?? show('visitedWithinDays')}
            value={draft.visitedWithinDays}
            onChange={(visitedWithinDays) => change({ visitedWithinDays })}
          />
        </div>

        <div className="max-w-xs">
          <Field label={text.minSpend} htmlFor="segment-spend" hint={text.minSpendHint} error={server.min_spend_satang ?? show('minSpend')}>
            <Input id="segment-spend" inputMode="decimal" value={draft.minSpend} onChange={(event) => change({ minSpend: event.target.value })} />
          </Field>
        </div>

        <CheckboxGroup
          legend={text.tiers}
          error={server.tiers}
          options={tierLabels}
          selected={draft.tiers}
          onChange={(tiers) => change({ tiers: tiers as LoyaltyTier[] })}
        />

        <SegmentedChoice
          legend={text.activePackage}
          options={text.activePackageOptions}
          value={draft.activePackage}
          onChange={(activePackage) => change({ activePackage })}
        />

        {branches.length > 1 && (
          <BranchSelect
            id="segment-branch"
            label={text.branch}
            allLabel={text.allBranches}
            hint={text.branchHint}
            error={server.branch_id}
            branches={branches}
            value={draft.branchId}
            onChange={(branchId) => change({ branchId })}
          />
        )}
      </section>

      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? text.saving : text.submit}
        </Button>
        <FormStatus message={message} savedText="" />
      </div>
    </form>
  );
}
