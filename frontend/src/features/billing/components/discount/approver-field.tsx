'use client';

import { Field, Input, Select } from '@/components/ui/field';
import { discountForm } from '../../content/discount-form';

export const OTHER_APPROVER = '__other';

export interface StaffOption {
  id: string;
  name: string;
}

interface Props {
  self: StaffOption;
  /** Active staff at the branch, or null when the session may not read the staff list. */
  staff: StaffOption[] | null;
  choice: string;
  otherId: string;
  error?: string;
  onChoice: (value: string) => void;
  onOtherId: (value: string) => void;
}

/**
 * Who approved the discount. Required, and deliberately not assumed to be whoever is keying it in:
 * the backend stores the approver and the person who saved it as two separate names.
 */
export function ApproverField({ self, staff, choice, otherId, error, onChoice, onOtherId }: Props) {
  const text = discountForm;
  const others = (staff ?? []).filter((member) => member.id !== self.id);

  return (
    <div className="space-y-2">
      <Field label={text.approver} htmlFor="discount-approver" error={choice !== OTHER_APPROVER ? error : undefined} hint={text.approverHint}>
        <Select id="discount-approver" value={choice} onChange={(event) => onChoice(event.target.value)}>
          <option value={self.id}>{text.approverSelf(self.name)}</option>
          {others.map((member) => (
            <option key={member.id} value={member.id}>
              {member.name}
            </option>
          ))}
          <option value={OTHER_APPROVER}>{text.approverOther}</option>
        </Select>
      </Field>

      {choice === OTHER_APPROVER && (
        <Field
          label={text.approverId}
          htmlFor="discount-approver-id"
          error={error}
          hint={staff === null ? text.approverNoList : undefined}
        >
          <Input
            id="discount-approver-id"
            value={otherId}
            maxLength={40}
            placeholder={text.approverIdPlaceholder}
            autoComplete="off"
            onChange={(event) => onOtherId(event.target.value)}
          />
        </Field>
      )}
    </div>
  );
}
