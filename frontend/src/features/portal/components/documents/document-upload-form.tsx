'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { uploadToSignedUrl } from '@/lib/api/signed-upload';
import { documentSentAction, startDocumentUploadAction } from '../../actions/document-actions';
import { petDocuments } from '../../content/pet-documents';
import { documentTypeOf, type DocumentKind } from '../../lib/document-rules';
import { refusalMessage } from '../../lib/form-error';
import { TextField } from '../shared/text-field';
import { DocumentFilePicker } from './document-file-picker';
import { DocumentKindField } from './document-kind-field';
import { DocumentSent } from './document-sent';

type Phase = 'idle' | 'starting' | 'uploading';

interface DocumentUploadFormProps {
  petId: string;
  /** Where "ดูประวัติการรักษา" goes, when this owner may see the pet's history. */
  recordsHref?: string;
}

/**
 * Sends one file for a pet. The backend first records it and hands back a signed URL, then the
 * browser puts the bytes there. A file whose bytes never arrive stays on the record as pending and
 * is not shown to anyone, so a failed upload is simply tried again.
 */
export function DocumentUploadForm({ petId, recordsHref }: DocumentUploadFormProps) {
  const text = petDocuments;
  const [kind, setKind] = useState<DocumentKind>('photo');
  const [title, setTitle] = useState('');
  const [file, setFile] = useState<File>();
  const [phase, setPhase] = useState<Phase>('idle');
  const [error, setError] = useState<string>();
  const [missingFile, setMissingFile] = useState(false);
  const [sent, setSent] = useState<string>();

  async function send(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);

    const contentType = file && documentTypeOf(file);
    setMissingFile(!file);
    if (!file || !contentType) return;

    try {
      setPhase('starting');
      const started = await startDocumentUploadAction({
        petId,
        fileName: file.name,
        contentType,
        sizeBytes: file.size,
        kind,
        title,
      });

      if (!started.ok) {
        setError(refusalMessage(started.error));
        return;
      }

      setPhase('uploading');
      if (!(await uploadToSignedUrl(started.data.upload, file))) {
        setError(text.uploadFailed);
        return;
      }

      await documentSentAction();
      setSent(title.trim() || file.name);
    } catch {
      setError(text.unreachable);
    } finally {
      setPhase('idle');
    }
  }

  function startOver() {
    setSent(undefined);
    setFile(undefined);
    setTitle('');
  }

  if (sent) {
    return <DocumentSent name={sent} recordsHref={recordsHref} onAnother={startOver} />;
  }

  const busy = phase !== 'idle';

  return (
    <form onSubmit={send} className="space-y-5">
      {error && <Alert>{error}</Alert>}

      <DocumentKindField value={kind} onChange={setKind} disabled={busy} />

      <DocumentFilePicker
        file={file}
        disabled={busy}
        error={missingFile && !file ? text.fileMissing : undefined}
        onChange={setFile}
      />

      <TextField
        id="document-title"
        label={text.titleLabel}
        hint={text.titleHint}
        value={title}
        maxLength={200}
        disabled={busy}
        onChange={(event) => setTitle(event.target.value)}
      />

      <button
        type="submit"
        disabled={busy}
        className="h-12 w-full rounded-xl bg-site-orange text-sm font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50 sm:w-auto sm:px-8"
      >
        {phase === 'starting' ? text.starting : phase === 'uploading' ? text.uploading : text.send}
      </button>
    </form>
  );
}
