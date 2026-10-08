'use client';

import { useId, useState, type KeyboardEvent } from 'react';
import { Field, Input } from '@/components/ui/field';
import type { DrugOption } from '@/features/prescriptions/types/drug-option.types';
import { consultationPage } from '../../content/consultation-page';
import { useDrugOptions } from '../../hooks/use-drug-options';
import { DrugLinkNote } from './drug-link-note';
import { DrugSuggestions } from './drug-suggestions';

interface DrugSearchFieldProps {
  branchId: string;
  name: string;
  item?: DrugOption;
  error?: string;
  /** The vet typed: the name is free text again, no longer tied to a stock item. */
  onType: (name: string) => void;
  onPick: (option: DrugOption) => void;
}

/**
 * The drug name, with the clinic's medicines suggested as the vet types (a contract extension,
 * docs/contract-extensions.md § 20). Picking one fills the name and ties the prescription to that
 * item, so the dispense takes and charges it. Typing over a picked name unties it: what the field
 * shows is always what will be written. A name that matches nothing can still be written.
 */
export function DrugSearchField({ branchId, name, item, error, onType, onPick }: DrugSearchFieldProps) {
  const label = consultationPage.prescriptions.fields.drug;
  const baseId = useId();
  const inputId = `${baseId}-drug`;
  const listId = `${baseId}-options`;
  const noteId = `${baseId}-note`;
  const optionId = (index: number) => `${baseId}-option-${index}`;

  const { options, state, search, clear } = useDrugOptions(branchId);
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  // Never a list for an empty field: what it would show belongs to text that is no longer there.
  const showList = open && state !== 'idle' && name.trim() !== '';

  function type(value: string) {
    onType(value);
    setActive(-1);
    setOpen(true);
    search(value);
  }

  function pick(option: DrugOption) {
    onPick(option);
    setOpen(false);
    setActive(-1);
    clear();
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Escape') {
      setOpen(false);
      return;
    }

    if (options.length === 0) return;

    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      setOpen(true);
      const step = event.key === 'ArrowDown' ? 1 : -1;
      setActive((current) => (current + step + options.length) % options.length);
    } else if (event.key === 'Enter' && showList) {
      const highlighted = options[active];
      if (!highlighted) return;

      // Enter picks the highlighted drug rather than submitting the form.
      event.preventDefault();
      pick(highlighted);
    }
  }

  return (
    <div className="space-y-1.5">
      <Field label={label} htmlFor={inputId} error={error}>
        <div className="relative">
          <Input
            id={inputId}
            value={name}
            maxLength={200}
            required
            autoComplete="off"
            role="combobox"
            aria-expanded={showList && options.length > 0}
            aria-controls={listId}
            aria-autocomplete="list"
            aria-activedescendant={showList && active >= 0 ? optionId(active) : undefined}
            aria-describedby={noteId}
            aria-invalid={error ? true : undefined}
            onChange={(event) => type(event.target.value)}
            onKeyDown={onKeyDown}
            onFocus={() => setOpen(true)}
            onBlur={() => setOpen(false)}
          />
          {showList && (
            <DrugSuggestions id={listId} options={options} state={state} active={active} optionId={optionId} onPick={pick} />
          )}
        </div>
      </Field>
      <DrugLinkNote id={noteId} name={name} item={item} />
    </div>
  );
}
