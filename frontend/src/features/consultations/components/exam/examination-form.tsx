'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/field';
import { recordExaminationAction } from '../../actions/consultation-actions';
import { consultationPage } from '../../content/consultation-page';
import { useIncomingFormDraft } from '../../hooks/use-form-draft';
import { usePartSubmit } from '../../hooks/use-part-submit';
import type { Finding } from '../../types/consultation.types';

type Answer = 'skipped' | 'normal' | 'abnormal';

interface Row {
  answer: Answer;
  note: string;
}

interface ExaminationFormProps {
  consultationId: string;
  findings: Finding[];
  editable: boolean;
}

/**
 * The whole examination form, one row per body system: normal, abnormal, or not examined. Saving
 * sends the whole form, which replaces what was saved before. Systems already on the case that are
 * not on the clinic's usual list are kept as rows, so re-saving never drops them.
 */
export function ExaminationForm({ consultationId, findings, editable }: ExaminationFormProps) {
  const text = consultationPage.exam;
  const systems = [...text.systems, ...findings.map((finding) => finding.body_system).filter((system) => !text.systems.includes(system))];

  const [rows, setRows] = useState<Record<string, Row>>(() =>
    Object.fromEntries(
      systems.map((system) => {
        const found = findings.find((finding) => finding.body_system === system);
        return [system, { answer: found ? (found.is_normal ? 'normal' : 'abnormal') : 'skipped', note: found?.note ?? '' }];
      }),
    ),
  );
  const [saved, setSaved] = useState(false);
  const { message, errors, pending, submit } = usePartSubmit();

  const update = (system: string, change: Partial<Row>) => {
    setSaved(false);
    setRows((current) => ({ ...current, [system]: { ...current[system]!, ...change } }));
  };

  // Findings drafted by the AI panel (docs/contract-extensions.md § 34) go onto the rows of the
  // same body systems. Nothing is saved until the vet presses the form's own save button.
  useIncomingFormDraft((draft) => {
    if (!editable) return;

    for (const finding of draft.findings) {
      if (systems.includes(finding.body_system)) {
        update(finding.body_system, { answer: finding.is_normal ? 'normal' : 'abnormal', note: finding.note ?? '' });
      }
    }
  });

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const answered = systems
      .filter((system) => rows[system]!.answer !== 'skipped')
      .map((system) => ({ bodySystem: system, isNormal: rows[system]!.answer === 'normal', note: rows[system]!.note }));

    submit(() => recordExaminationAction({ consultationId, findings: answered }), () => setSaved(true));
  }

  const answers: { value: Answer; label: string; tone: string }[] = [
    { value: 'normal', label: text.normal, tone: 'peer-checked:border-emerald-500 peer-checked:bg-emerald-50 peer-checked:text-emerald-800' },
    { value: 'abnormal', label: text.abnormal, tone: 'peer-checked:border-red-400 peer-checked:bg-red-50 peer-checked:text-red-800' },
    { value: 'skipped', label: text.skipped, tone: 'peer-checked:border-ink-400 peer-checked:bg-ink-100 peer-checked:text-ink-800' },
  ];

  return (
    <form onSubmit={save} className="space-y-3">
      <p className="text-sm text-ink-500">{text.hint}</p>
      {/* A refusal may name `findings` or one row of it (`findings[2].note`); either way, one line. */}
      {(message || Object.keys(errors).length > 0) && (
        <p role="alert" className="text-sm text-red-700">{message ?? Object.values(errors)[0]}</p>
      )}

      <div className="divide-y divide-ink-100 rounded-lg border border-ink-200">
        {systems.map((system) => {
          const row = rows[system]!;
          return (
            <fieldset key={system} disabled={!editable} className="flex flex-wrap items-center gap-x-3 gap-y-2 px-3 py-2">
              <legend className="sr-only">{system}</legend>
              <span className="w-40 shrink-0 text-sm font-medium text-ink-800">{system}</span>
              <div className="flex gap-1">
                {answers.map((option) => (
                  <label key={option.value} className="cursor-pointer">
                    <input
                      type="radio"
                      name={`exam-${system}`}
                      value={option.value}
                      checked={row.answer === option.value}
                      onChange={() => update(system, { answer: option.value })}
                      className="peer sr-only"
                    />
                    <span className={`inline-block rounded-md border border-ink-200 px-2.5 py-1 text-xs text-ink-600 peer-focus-visible:outline-2 peer-focus-visible:outline-brand-600 ${option.tone}`}>
                      {option.label}
                    </span>
                  </label>
                ))}
              </div>
              {row.answer === 'abnormal' && (
                <Input
                  aria-label={`${system} ${text.notePlaceholder}`}
                  placeholder={text.notePlaceholder}
                  value={row.note}
                  maxLength={500}
                  className="h-8 min-w-48 flex-1"
                  onChange={(event) => update(system, { note: event.target.value })}
                />
              )}
            </fieldset>
          );
        })}
      </div>

      {editable && (
        <div className="flex items-center justify-end gap-3">
          {saved && <p role="status" className="text-sm text-emerald-700">{text.saved}</p>}
          <Button type="submit" disabled={pending}>
            {pending ? consultationPage.saving : text.submit}
          </Button>
        </div>
      )}
    </form>
  );
}
