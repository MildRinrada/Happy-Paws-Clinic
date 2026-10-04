'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Select } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { fieldErrors } from '@/lib/actions/field-errors';
import { uploadToSignedUrl } from '@/lib/api/signed-upload';
import { startPetDocumentAction } from '../../actions/document-actions';
import { petDocuments } from '../../content/pet-documents';
import { petForms } from '../../content/pet-forms';
import { DOCUMENT_KINDS, DOCUMENT_MAX_BYTES, documentTypeOf } from '../../lib/document-rules';
import type { PetDocumentKind } from '../../types/pet.types';

type Phase = 'idle' | 'starting' | 'sending';

/**
 * Adds a file to the record in two moves: the backend records it and hands back a signed URL,
 * then the browser sends the bytes straight there. The file is checked here first so a wrong type
 * or a 20 MB scan is refused before anything is recorded.
 */
export function UploadDocumentForm({ petId }: { petId: string }) {
  const text = petDocuments.upload;
  const [open, setOpen] = useState(false);
  const [file, setFile] = useState<File>();
  const [kind, setKind] = useState<PetDocumentKind>('lab_result');
  const [title, setTitle] = useState('');
  const [visibleToOwner, setVisibleToOwner] = useState(false);
  const [phase, setPhase] = useState<Phase>('idle');
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string>();
  const [done, setDone] = useState<string>();

  function close() {
    setOpen(false);
    setFile(undefined);
    setTitle('');
    setVisibleToOwner(false);
    setErrors({});
    setMessage(undefined);
  }

  async function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrors({});
    setMessage(undefined);
    setDone(undefined);

    if (!file) return setErrors({ file: text.pickFile });
    const contentType = documentTypeOf(file);
    if (!contentType) return setErrors({ file: text.wrongType });
    if (file.size > DOCUMENT_MAX_BYTES) return setErrors({ file: text.tooLarge });

    try {
      setPhase('starting');
      const started = await startPetDocumentAction({
        petId,
        fileName: file.name,
        contentType,
        sizeBytes: file.size,
        kind,
        title,
        visibleToOwner,
      });

      if (!started.ok) {
        const byField = fieldErrors(started.error);
        setErrors(byField);
        if (Object.keys(byField).length === 0) setMessage(started.error.message);
        return;
      }

      setPhase('sending');
      if (!(await uploadToSignedUrl(started.data.upload, file))) {
        setMessage(text.sendFailed);
        return;
      }

      setDone(text.done(file.name));
      close();
    } catch {
      setMessage(petForms.unreachable);
    } finally {
      setPhase('idle');
    }
  }

  if (!open) {
    return (
      <div className="space-y-2">
        <FormStatus saved={Boolean(done)} savedText={done ?? ''} />
        <Button variant="secondary" size="sm" onClick={() => { setDone(undefined); setOpen(true); }}>
          {text.open}
        </Button>
      </div>
    );
  }

  const busy = phase !== 'idle';
  // The backend names the file's fields; any of them is about the file the person picked.
  const fileError = errors.file ?? errors.file_name ?? errors.content_type ?? errors.size_bytes;

  return (
    <form onSubmit={send} className="space-y-3 rounded-lg border border-ink-200 bg-ink-50/60 p-4">
      <Field label={text.file} htmlFor="document-file" hint={text.fileHint} error={fileError}>
        <Input
          id="document-file"
          type="file"
          accept="image/jpeg,image/png,image/webp,image/heic,.heic,application/pdf"
          required
          className="py-1.5 file:mr-3 file:rounded-md file:border-0 file:bg-ink-100 file:px-2.5 file:py-1 file:text-sm file:text-ink-800"
          aria-invalid={Boolean(fileError)}
          onChange={(event) => setFile(event.target.files?.[0])}
        />
      </Field>

      <Field label={text.kind} htmlFor="document-kind" error={errors.kind}>
        <Select id="document-kind" value={kind} onChange={(event) => setKind(event.target.value as PetDocumentKind)}>
          {DOCUMENT_KINDS.map((key) => (
            <option key={key} value={key}>
              {text.kinds[key]}
            </option>
          ))}
        </Select>
      </Field>

      <Field label={text.documentTitle} htmlFor="document-title" error={errors.title}>
        <Input id="document-title" value={title} maxLength={200} onChange={(event) => setTitle(event.target.value)} />
      </Field>

      <Checkbox label={text.visibleToOwner} checked={visibleToOwner} onChange={(event) => setVisibleToOwner(event.target.checked)} />

      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} savedText="" />
        <Button variant="ghost" size="sm" onClick={close} disabled={busy}>
          {petForms.cancel}
        </Button>
        <Button type="submit" size="sm" disabled={busy}>
          {phase === 'starting' ? text.starting : phase === 'sending' ? text.sending : text.submit}
        </Button>
      </div>
    </form>
  );
}
