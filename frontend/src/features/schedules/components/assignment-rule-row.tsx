'use client';

import { Field, Input, Select } from '@/components/ui/field';
import { assignmentRulesText } from '../content/assignment-rules';
import { SPECIES_MAX_LENGTH, type RuleDraft, type RuleProblem } from '../lib/assignment-rule-draft';
import { ReorderButtons } from './reorder-buttons';
import { RuleStaffPicker } from './rule-staff-picker';

interface AssignmentRuleRowProps {
  rule: RuleDraft;
  index: number;
  count: number;
  people: { id: string; name: string }[];
  services: { id: string; name: string }[];
  /** The id of the shared list of species suggestions. */
  speciesListId: string;
  problem?: RuleProblem;
  disabled?: boolean;
  onChange: (rule: RuleDraft) => void;
  onMove: (step: -1 | 1) => void;
  onRemove: () => void;
}

/** One rule: which cases (species, service, or both) and who takes them first. */
export function AssignmentRuleRow({
  rule,
  index,
  count,
  people,
  services,
  speciesListId,
  problem,
  disabled,
  onChange,
  onMove,
  onRemove,
}: AssignmentRuleRowProps) {
  const text = assignmentRulesText;
  const label = text.rule(index + 1);
  const prefix = `rule-${index}`;
  // A rule saved earlier may name a service the list here does not hold (not offered at this branch).
  const unknownService = rule.serviceId && !services.some((service) => service.id === rule.serviceId);

  return (
    <li className={`rounded-lg border p-3 ${problem ? 'border-red-300 bg-red-50/40' : 'border-ink-200 bg-ink-50/40'}`}>
      <div className="mb-2 flex items-center justify-between gap-2">
        <p className="text-sm font-semibold text-ink-900">{label}</p>
        <ReorderButtons
          label={label}
          isFirst={index === 0}
          isLast={index === count - 1}
          removeLabel={text.removeRule}
          disabled={disabled}
          onMove={onMove}
          onRemove={onRemove}
        />
      </div>

      <div className="grid gap-3 md:grid-cols-[1fr_1fr_1.4fr]">
        <Field label={text.species} htmlFor={`${prefix}-species`} hint={text.speciesHint}>
          <Input
            id={`${prefix}-species`}
            list={speciesListId}
            value={rule.species}
            maxLength={SPECIES_MAX_LENGTH}
            placeholder={text.speciesPlaceholder}
            disabled={disabled}
            className="h-9"
            onChange={(event) => onChange({ ...rule, species: event.target.value })}
          />
        </Field>

        <Field label={text.service} htmlFor={`${prefix}-service`}>
          <Select
            id={`${prefix}-service`}
            value={rule.serviceId}
            disabled={disabled}
            className="h-9"
            onChange={(event) => onChange({ ...rule, serviceId: event.target.value })}
          >
            <option value="">{text.anyService}</option>
            {unknownService && <option value={rule.serviceId}>{rule.serviceId}</option>}
            {services.map((service) => (
              <option key={service.id} value={service.id}>
                {service.name}
              </option>
            ))}
          </Select>
        </Field>

        <Field label={text.staff} htmlFor={`${prefix}-staff`} hint={text.staffHint}>
          <RuleStaffPicker
            id={`${prefix}-staff`}
            people={people}
            selected={rule.staffIds}
            disabled={disabled}
            onChange={(staffIds) => onChange({ ...rule, staffIds })}
          />
        </Field>
      </div>

      {problem && (
        <p role="alert" className="mt-2 text-sm text-red-700">
          {text[problem]}
        </p>
      )}
    </li>
  );
}
