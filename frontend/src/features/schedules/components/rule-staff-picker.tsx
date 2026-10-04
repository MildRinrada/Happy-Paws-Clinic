'use client';

import { Select } from '@/components/ui/field';
import { assignmentRulesText } from '../content/assignment-rules';
import { MAX_STAFF_PER_RULE, moved } from '../lib/assignment-rule-draft';
import { ReorderButtons } from './reorder-buttons';

interface RuleStaffPickerProps {
  id: string;
  people: { id: string; name: string }[];
  selected: string[];
  onChange: (staffIds: string[]) => void;
  disabled?: boolean;
}

/** Who takes the rule's cases, most preferred first: chosen one at a time, then put in order. */
export function RuleStaffPicker({ id, people, selected, onChange, disabled }: RuleStaffPickerProps) {
  const text = assignmentRulesText;
  const names = new Map(people.map((person) => [person.id, person.name]));
  const left = people.filter((person) => !selected.includes(person.id));

  return (
    <div className="space-y-2">
      {selected.length > 0 && (
        <ol className="divide-y divide-ink-100 rounded-lg border border-ink-200 bg-white">
          {selected.map((staffId, index) => {
            const name = names.get(staffId) ?? staffId;

            return (
              <li key={staffId} className="flex items-center justify-between gap-2 py-1 pl-3 pr-1 text-sm">
                <span className="min-w-0 truncate text-ink-800">
                  <span className="mr-1.5 tabular-nums text-ink-400">{index + 1}.</span>
                  {name}
                </span>
                <ReorderButtons
                  label={name}
                  isFirst={index === 0}
                  isLast={index === selected.length - 1}
                  disabled={disabled}
                  onMove={(step) => onChange(moved(selected, index, index + step))}
                  onRemove={() => onChange(selected.filter((other) => other !== staffId))}
                />
              </li>
            );
          })}
        </ol>
      )}

      {selected.length < MAX_STAFF_PER_RULE && (
        <Select
          id={id}
          value=""
          disabled={disabled || left.length === 0}
          className="h-9"
          onChange={(event) => event.target.value && onChange([...selected, event.target.value])}
        >
          <option value="">{left.length === 0 ? text.noStaffLeft : text.addStaff}</option>
          {left.map((person) => (
            <option key={person.id} value={person.id}>
              {person.name}
            </option>
          ))}
        </Select>
      )}
    </div>
  );
}
