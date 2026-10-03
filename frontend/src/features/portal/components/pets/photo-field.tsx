'use client';

/* eslint-disable @next/next/no-img-element -- previews are local object URLs and short-lived signed links. */

import { useEffect, useState } from 'react';
import { petEdit } from '../../content/pet-edit';
import { PHOTO_MAX_BYTES, PHOTO_TYPES, isPhotoType } from '../../lib/photo-rules';

interface PhotoFieldProps {
  currentUrl?: string | null;
  name: string;
  onChange: (file: File | undefined) => void;
}

/** The pet's photo, a button to choose another, and a preview of the new one before saving. */
export function PhotoField({ currentUrl, name, onChange }: PhotoFieldProps) {
  const [preview, setPreview] = useState<string>();
  const [error, setError] = useState<string>();

  // Free the preview's memory when it is replaced or the form goes away.
  useEffect(() => () => {
    if (preview) URL.revokeObjectURL(preview);
  }, [preview]);

  function choose(file: File | undefined) {
    setError(undefined);

    if (!file) return;
    if (!isPhotoType(file.type)) return setError(petEdit.photoWrongType);
    if (file.size > PHOTO_MAX_BYTES) return setError(petEdit.photoTooBig);

    setPreview(URL.createObjectURL(file));
    onChange(file);
  }

  const shown = preview ?? currentUrl;

  return (
    <div className="flex items-center gap-4">
      {shown ? (
        <img src={shown} alt="" className="size-20 shrink-0 rounded-full object-cover" />
      ) : (
        <span aria-hidden="true" className="flex size-20 shrink-0 items-center justify-center rounded-full bg-brand-100 text-2xl font-bold text-site-teal">
          {name.trim().charAt(0) || '?'}
        </span>
      )}

      <div className="space-y-1">
        <p className="text-sm font-semibold text-ink-900">{petEdit.photo}</p>
        <label className="inline-flex h-9 cursor-pointer items-center rounded-lg border border-ink-200 bg-white px-3 text-sm font-semibold text-ink-700 hover:bg-ink-50">
          {petEdit.photoPick}
          <input
            type="file"
            accept={PHOTO_TYPES.join(',')}
            className="sr-only"
            onChange={(event) => choose(event.target.files?.[0])}
          />
        </label>
        {error ? (
          <p role="alert" className="text-xs text-red-700">
            {error}
          </p>
        ) : (
          <p className="text-xs text-site-muted">{petEdit.photoHint}</p>
        )}
      </div>
    </div>
  );
}
