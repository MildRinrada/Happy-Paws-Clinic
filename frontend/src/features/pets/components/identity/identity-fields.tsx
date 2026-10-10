'use client';

import type { PetSex } from '../../types/pet.types';
import { BirthDateFields } from './birth-date-fields';
import { NeuteredSelect, type NeuteredChoice } from './neutered-select';
import { SexSelect } from './sex-select';

/** Sex, birth date and neuter status as a form holds them. Empty and "unknown" mean not known. */
export interface IdentityValues {
  sex: PetSex;
  birthDate: string;
  birthDateEstimated: boolean;
  neutered: NeuteredChoice;
}

export const unknownIdentity: IdentityValues = { sex: 'unknown', birthDate: '', birthDateEstimated: false, neutered: 'unknown' };

interface IdentityFieldsProps {
  values: IdentityValues;
  onChange: (values: IdentityValues) => void;
  errors: Record<string, string>;
  /** A closed record keeps its neuter status. */
  neuteredLocked?: boolean;
}

/** The three optional facts staff record about who a patient is (docs/contract-extensions.md § 28). */
export function IdentityFields({ values, onChange, errors, neuteredLocked }: IdentityFieldsProps) {
  const set = (patch: Partial<IdentityValues>) => onChange({ ...values, ...patch });

  return (
    <>
      <SexSelect value={values.sex} onChange={(sex) => set({ sex })} error={errors.sex} />
      <NeuteredSelect
        value={values.neutered}
        onChange={(neutered) => set({ neutered })}
        error={errors.neutered}
        disabled={neuteredLocked}
      />
      <div className="sm:col-span-2">
        <BirthDateFields
          value={values.birthDate}
          estimated={values.birthDateEstimated}
          onChange={(birthDate) => set({ birthDate, birthDateEstimated: birthDate ? values.birthDateEstimated : false })}
          onEstimatedChange={(birthDateEstimated) => set({ birthDateEstimated })}
          error={errors.birth_date}
          estimatedError={errors.birth_date_estimated}
        />
      </div>
    </>
  );
}
