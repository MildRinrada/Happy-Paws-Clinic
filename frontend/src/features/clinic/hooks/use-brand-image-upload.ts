'use client';

import { useEffect, useState } from 'react';
import { formMessages } from '@/features/settings/content/form-messages';
import { uploadToSignedUrl } from '@/lib/api/signed-upload';
import { requestBrandImageAction } from '../actions/clinic-setup-actions';
import { clinicLogoText } from '../content/clinic-logo-settings';
import { BRAND_IMAGE_MAX_BYTES, brandImageTypeOf, readImageSize } from '../lib/brand-image-rules';
import type { BrandImageKind } from '../types/settings.types';

/** A file sent to the storage: the value to save, and the local preview of it. */
export interface PickedImage {
  value: string;
  preview: string;
  width?: number;
  height?: number;
}

/**
 * Sends a chosen logo or tab icon (docs/contract-extensions.md § 50): checks the type and size,
 * asks the API for a signed URL, sends the file straight there from the browser, and hands back
 * the value for the form. The website changes only when the form is saved.
 */
export function useBrandImageUpload(kind: BrandImageKind, onUploaded: (value: string) => void) {
  const [busy, setBusy] = useState(false);
  const [problem, setProblem] = useState<string>();
  const [picked, setPicked] = useState<PickedImage>();

  // A picked file's preview lives in browser memory until the next pick or the page closes.
  useEffect(() => () => {
    if (picked) URL.revokeObjectURL(picked.preview);
  }, [picked]);

  async function upload(file: File) {
    setProblem(undefined);
    const contentType = brandImageTypeOf(file);
    if (!contentType) return setProblem(clinicLogoText.wrongType);
    if (file.size > BRAND_IMAGE_MAX_BYTES) return setProblem(clinicLogoText.tooBig);

    setBusy(true);
    const preview = URL.createObjectURL(file);
    try {
      const started = await requestBrandImageAction({ kind, contentType, sizeBytes: file.size });
      if (!started.ok) return fail(preview, started.error.message);
      if (!(await uploadToSignedUrl(started.data.upload, file))) return fail(preview, clinicLogoText.failed);

      const size = await readImageSize(preview);
      setPicked({ value: started.data.value, preview, ...size });
      onUploaded(started.data.value);
    } catch {
      fail(preview, formMessages.unreachable);
    } finally {
      setBusy(false);
    }
  }

  function fail(preview: string, message: string) {
    URL.revokeObjectURL(preview);
    setProblem(message);
  }

  return { busy, problem, picked, upload, clearProblem: () => setProblem(undefined) };
}
