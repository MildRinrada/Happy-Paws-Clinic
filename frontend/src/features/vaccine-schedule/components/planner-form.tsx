import { useId, type FormEvent } from 'react';
import { speciesChoices, vaccinePage } from '../content/vaccine-schedule-page';
import type { InputErrors } from '../lib/validate-input';
import type { Species } from '../types/vaccine-schedule.types';
import { DateField } from './date-field';
import { SpeciesOption } from './species-option';

interface Props {
  species: Species;
  birthDate: string;
  lastVaccineDate: string;
  errors: InputErrors;
  onSpecies: (value: Species) => void;
  onBirthDate: (value: string) => void;
  onLastVaccineDate: (value: string) => void;
  onSubmit: () => void;
}

/** The three questions: dog or cat, born when, and last vaccinated when (if known). */
export function PlannerForm(props: Props) {
  const text = vaccinePage.form;
  const groupName = useId();
  const headingId = useId();

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    props.onSubmit();
  };

  return (
    <form
      aria-labelledby={headingId}
      noValidate
      onSubmit={submit}
      className="space-y-6 rounded-2xl border border-site-border bg-white p-5 shadow-[0_8px_24px_rgba(19,50,43,0.08)] sm:p-7 print:hidden"
    >
      <h2 id={headingId} className="text-xl font-semibold">
        {text.heading}
      </h2>

      <fieldset>
        <legend className="text-[15px] font-semibold">{text.speciesLegend}</legend>
        <div className="mt-2 grid grid-cols-2 gap-3">
          {speciesChoices.map((choice) => (
            <SpeciesOption
              key={choice.value}
              name={groupName}
              value={choice.value}
              label={choice.label}
              detail={choice.detail}
              checked={props.species === choice.value}
              onChange={props.onSpecies}
            />
          ))}
        </div>
      </fieldset>

      <DateField
        label={text.birthLabel}
        hint={text.birthHint}
        value={props.birthDate}
        error={props.errors.birthDate}
        required
        onChange={props.onBirthDate}
      />

      <DateField
        label={text.lastLabel}
        hint={text.lastHint}
        value={props.lastVaccineDate}
        error={props.errors.lastVaccineDate}
        onChange={props.onLastVaccineDate}
      />

      <button
        type="submit"
        className="inline-flex h-[52px] w-full items-center justify-center rounded-[10px] bg-site-orange px-5 text-base font-bold text-white transition hover:brightness-95 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-site-teal"
      >
        {text.submit}
      </button>
    </form>
  );
}
