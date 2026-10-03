'use client';

import { useEffect, useRef, useState } from 'react';
import { Field, Textarea } from '@/components/ui/field';
import { formatTime } from '@/lib/format/datetime';
import { saveNotesAction } from '../../actions/consultation-actions';
import { consultationPage } from '../../content/consultation-page';
import type { ConsultationNotes } from '../../types/consultation.types';

const FIELDS: (keyof ConsultationNotes)[] = ['chief_complaint', 'subjective', 'objective', 'assessment', 'plan'];

/** Saved this long after the vet stops typing. The contract asks for an autosave every ten seconds. */
const AUTOSAVE_MS = 3000;

interface NotesFormProps {
  consultationId: string;
  initial: ConsultationNotes;
  editable: boolean;
}

/**
 * The narrative of the case, in SOAP order, saved by itself.
 *
 * Only the fields touched since the last save are sent, so two people typing in different boxes
 * do not overwrite each other. A refused save keeps the text on screen and says so.
 */
export function NotesForm({ consultationId, initial, editable }: NotesFormProps) {
  const text = consultationPage.notes;
  const [values, setValues] = useState(initial);
  const [dirty, setDirty] = useState<Set<keyof ConsultationNotes>>(new Set());
  const [savedAt, setSavedAt] = useState<string>();
  const [error, setError] = useState<string>();
  const [saving, setSaving] = useState(false);
  const latest = useRef(values);
  latest.current = values;

  useEffect(() => {
    if (dirty.size === 0 || saving) return;

    const timer = setTimeout(async () => {
      const fields = [...dirty];
      const changes = Object.fromEntries(fields.map((field) => [field, latest.current[field]]));

      setSaving(true);
      setError(undefined);
      try {
        const result = await saveNotesAction({ consultationId, changes });
        if (result.ok) {
          setDirty((current) => new Set([...current].filter((field) => !fields.includes(field) || latest.current[field] !== changes[field])));
          setSavedAt(formatTime(new Date().toISOString()));
        } else {
          setError(result.error.message);
        }
      } catch {
        setError(consultationPage.unreachable);
      } finally {
        setSaving(false);
      }
    }, AUTOSAVE_MS);

    return () => clearTimeout(timer);
  }, [dirty, saving, consultationId]);

  function change(field: keyof ConsultationNotes, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    setDirty((current) => new Set(current).add(field));
  }

  return (
    <div className="space-y-4">
      {FIELDS.map((field) => (
        <Field key={field} label={text.fields[field]} htmlFor={`notes-${field}`}>
          <Textarea
            id={`notes-${field}`}
            value={values[field]}
            rows={field === 'chief_complaint' ? 2 : 3}
            maxLength={field === 'chief_complaint' ? 2000 : 8000}
            readOnly={!editable}
            onChange={(event) => change(field, event.target.value)}
          />
        </Field>
      ))}

      {editable && (
        <p role="status" className={`text-sm ${error ? 'text-red-700' : 'text-ink-500'}`}>
          {error ?? (saving ? consultationPage.saving : dirty.size > 0 ? text.unsaved : savedAt ? text.autosaved(savedAt) : '')}
        </p>
      )}
    </div>
  );
}
