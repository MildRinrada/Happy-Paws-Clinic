'use client';

import { useState } from 'react';
import { petDocuments } from '../../content/pet-documents';
import { DOCUMENT_ACCEPT, DOCUMENT_MAX_BYTES, documentTypeOf, fileSizeLabel } from '../../lib/document-rules';

interface DocumentFilePickerProps {
  file?: File;
  onChange: (file: File | undefined) => void;
  /** A message from the form, such as "choose a file first". */
  error?: string;
  disabled?: boolean;
}

/**
 * One file for the clinic: a button to choose it, its name and size once chosen, and the rules.
 * A file of the wrong type or size is refused here, before anything is sent.
 */
export function DocumentFilePicker({ file, onChange, error, disabled }: DocumentFilePickerProps) {
  const text = petDocuments;
  const [refusal, setRefusal] = useState<string>();
  const shownError = refusal ?? error;

  function choose(picked: File | undefined) {
    setRefusal(undefined);

    if (!picked) return;
    if (!documentTypeOf(picked)) return setRefusal(text.fileWrongType);
    if (picked.size > DOCUMENT_MAX_BYTES) return setRefusal(text.fileTooBig);

    onChange(picked);
  }

  return (
    <div className="space-y-1.5">
      <p className="text-sm font-semibold text-ink-900">{text.file}</p>
      <div className="flex flex-wrap items-center gap-3">
        <label
          className={`inline-flex h-10 items-center rounded-lg border border-ink-200 bg-white px-3 text-sm font-semibold text-ink-700 hover:bg-ink-50 ${
            disabled ? 'pointer-events-none opacity-50' : 'cursor-pointer'
          }`}
        >
          {file ? text.fileChange : text.filePick}
          <input
            type="file"
            accept={DOCUMENT_ACCEPT}
            className="sr-only"
            disabled={disabled}
            onChange={(event) => {
              choose(event.target.files?.[0]);
              event.target.value = '';
            }}
          />
        </label>
        {file && (
          <p className="min-w-0 text-sm text-ink-900">
            <span className="break-all font-medium">{file.name}</span>{' '}
            <span className="text-site-muted">({fileSizeLabel(file.size)})</span>
          </p>
        )}
      </div>
      {shownError ? (
        <p role="alert" className="text-xs text-red-700">
          {shownError}
        </p>
      ) : (
        <p className="text-xs text-site-muted">{text.fileHint}</p>
      )}
    </div>
  );
}
