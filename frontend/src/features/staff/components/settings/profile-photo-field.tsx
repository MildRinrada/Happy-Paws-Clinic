'use client';

import { useEffect, useId, useRef, useState, type ChangeEvent } from 'react';
import { buttonClass } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { uploadToSignedUrl } from '@/lib/api/signed-upload';
import { requestStaffPhotoAction } from '../../actions/public-profile-actions';
import { publicProfileText } from '../../content/public-profile';
import { PHOTO_MAX_BYTES, isUploadedPhoto, photoTypeOf } from '../../lib/photo-rules';
import type { StaffPublicProfile } from '../../types/staff.types';
import { ProfilePhotoPathField } from './profile-photo-path-field';
import { ProfilePhotoPreview } from './profile-photo-preview';

interface ProfilePhotoFieldProps {
  staffId: string;
  /** The profile as loaded: its `photo_url` previews the stored photo. */
  saved?: StaffPublicProfile;
  /** The `photo_path` the form will save. */
  value: string;
  onChange: (photoPath: string) => void;
  error?: string;
}

/**
 * The photo on the team page (docs/contract-extensions.md § 32). Picking a file asks the API for a
 * signed URL, sends the file straight there from the browser, and puts the answered `photo_path`
 * in the form; the website changes only when the form is saved. Removing clears the field the same
 * way, since the profile is replaced whole.
 */
export function ProfilePhotoField({ staffId, saved, value, onChange, error }: ProfilePhotoFieldProps) {
  const text = publicProfileText.photo;
  const id = useId();
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [problem, setProblem] = useState<string>();
  const [status, setStatus] = useState<string>();
  const [picked, setPicked] = useState<{ photoPath: string; preview: string }>();

  // A picked file's preview lives in browser memory until the next pick or the form closes.
  useEffect(() => () => {
    if (picked) URL.revokeObjectURL(picked.preview);
  }, [picked]);

  async function upload(file: File) {
    const contentType = photoTypeOf(file);
    if (!contentType) return setProblem(text.wrongType);
    if (file.size > PHOTO_MAX_BYTES) return setProblem(text.tooBig);

    setBusy(true);
    try {
      const started = await requestStaffPhotoAction({ id: staffId, contentType, sizeBytes: file.size });
      if (!started.ok) return setProblem(started.error.message);
      if (!(await uploadToSignedUrl(started.data.upload, file))) return setProblem(text.failed);

      setPicked({ photoPath: started.data.photo_path, preview: URL.createObjectURL(file) });
      onChange(started.data.photo_path);
      setStatus(text.uploaded);
    } catch {
      setProblem(formMessages.unreachable);
    } finally {
      setBusy(false);
      if (input.current) input.current.value = '';
    }
  }

  function choose(event: ChangeEvent<HTMLInputElement>) {
    setProblem(undefined);
    setStatus(undefined);
    const file = event.target.files?.[0];
    if (file) void upload(file);
  }

  function remove() {
    setProblem(undefined);
    onChange('');
    setStatus(text.removed);
  }

  const uploaded = isUploadedPhoto(value);
  const preview =
    picked && picked.photoPath === value
      ? picked.preview
      : value !== '' && value === saved?.photo_path && saved.photo_url
        ? saved.photo_url
        : value.startsWith('/') ? value : undefined;

  return (
    <div className="space-y-3">
      <p className="text-sm font-medium text-ink-700">{text.label}</p>
      <ProfilePhotoPreview src={preview} note={value.startsWith('/') ? `${text.sitePath} (${value})` : undefined} />
      <p id={`${id}-hint`} className="text-xs text-ink-600">
        {text.hint}
      </p>

      <div className="flex flex-wrap items-center gap-3">
        <label className={buttonClass('secondary', 'sm', busy ? 'pointer-events-none opacity-60' : 'cursor-pointer')}>
          {busy ? text.uploading : value ? text.change : text.choose}
          <input
            ref={input}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="sr-only"
            disabled={busy}
            aria-describedby={`${id}-hint`}
            onChange={choose}
          />
        </label>

        {value && (
          <button type="button" className={buttonClass('secondary', 'sm')} disabled={busy} onClick={remove}>
            {text.remove}
          </button>
        )}
      </div>

      {status && (
        <p role="status" className="text-sm text-emerald-700">
          {status}
        </p>
      )}
      {(problem ?? error) && (
        <p role="alert" className="text-sm text-red-700">
          {problem ?? error}
        </p>
      )}

      <ProfilePhotoPathField value={uploaded ? '' : value} onChange={onChange} />
    </div>
  );
}
