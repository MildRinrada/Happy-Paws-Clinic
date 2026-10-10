import { petIdentity } from '../content/pet-identity';
import { formatBirthDate, petAge } from './pet-age';

/** The facts a pet card shows for sex, birth date, age and neuter status. Unknown ones are left out. */
export interface IdentityFact {
  label: string;
  value: string;
}

interface IdentityFields {
  sex?: string;
  birth_date?: string | null;
  birth_date_estimated?: boolean;
  neutered?: boolean | null;
}

/** Turns the three § 28 fields into labelled lines, in the order a vet reads them. */
export function identityFacts(pet: IdentityFields): IdentityFact[] {
  const text = petIdentity;
  const facts: IdentityFact[] = [];
  const estimated = Boolean(pet.birth_date_estimated);

  if (pet.sex && pet.sex !== 'unknown') {
    facts.push({ label: text.sex.label, value: text.sex.options[pet.sex] ?? pet.sex });
  }

  if (pet.birth_date) {
    const date = formatBirthDate(pet.birth_date);
    facts.push({ label: text.birthDate.label, value: estimated ? text.birthDate.about(date) : date });

    const age = petAge(pet.birth_date);
    if (age) facts.push({ label: text.age.label, value: text.age.value(age.years, age.months, estimated) });
  }

  if (typeof pet.neutered === 'boolean') {
    facts.push({ label: text.neutered.label, value: text.neutered.options[pet.neutered ? 'yes' : 'no'] ?? '' });
  }

  return facts;
}

/** The select value for a neuter status that may be unknown. */
export function neuteredChoice(neutered: boolean | null | undefined): 'yes' | 'no' | 'unknown' {
  if (neutered === true) return 'yes';
  if (neutered === false) return 'no';
  return 'unknown';
}
