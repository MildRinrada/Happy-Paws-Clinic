'use client';

import { Field, Select } from '@/components/ui/field';
import type { BranchOption } from '../../types/crm.types';

interface Props {
  id: string;
  label: string;
  /** The "every branch" choice, or undefined when the session must pick one of its own. */
  allLabel?: string;
  hint?: string;
  error?: string;
  branches: BranchOption[];
  value: string;
  onChange: (value: string) => void;
}

/** Which branch something is limited to. '' stands for every branch. */
export function BranchSelect({ id, label, allLabel, hint, error, branches, value, onChange }: Props) {
  return (
    <Field label={label} htmlFor={id} hint={hint} error={error}>
      <Select id={id} value={value} onChange={(event) => onChange(event.target.value)}>
        {allLabel !== undefined && <option value="">{allLabel}</option>}
        {branches.map((branch) => (
          <option key={branch.id} value={branch.id}>
            {branch.name}
          </option>
        ))}
      </Select>
    </Field>
  );
}
