'use client';

import { useId } from 'react';
import { buttonClass } from '@/components/ui/button';
import { clinicLogoText } from '../../../content/clinic-logo-settings';
import { ImageDropZone } from './image-drop-zone';
import { UploadNotes } from './upload-notes';

interface TabIconDetailsProps {
  /** The `tab_icon_url` the form will save. */
  value: string;
  /** Where the browser loads it from, for the small preview. */
  src?: string;
  busy: boolean;
  status?: string;
  warning?: string;
  problem?: string;
  onFile: (file: File) => void;
  onRemove: () => void;
}

/**
 * The optional square icon for the browser tab (docs/contract-extensions.md § 50), folded away
 * because most clinics never need it: without one the tab shows the logo fitted into a square.
 */
export function TabIconDetails({ value, src, busy, status, warning, problem, onFile, onRemove }: TabIconDetailsProps) {
  const text = clinicLogoText.tabIcon;
  const hintId = useId();

  return (
    <details className="rounded-xl border border-ink-200 px-4 py-3 text-sm" open={value !== '' || undefined}>
      <summary className="cursor-pointer font-medium text-ink-700">{text.summary}</summary>
      <div className="mt-3 space-y-3">
        <p className="text-ink-600">{text.intro}</p>
        <div className="flex items-center gap-3">
          {/* eslint-disable-next-line @next/next/no-img-element -- a local preview, or the API's redirect to a signed link. */}
          {src && <img src={src} alt="" className="size-10 rounded-lg border border-ink-200 object-contain" />}
          <span className="text-ink-700">{value ? text.set : text.none}</span>
        </div>
        <p id={hintId} className="text-xs text-ink-600">
          {text.hint}
        </p>
        <ImageDropZone label={text.heading} busy={busy} hasImage={Boolean(value)} hintId={hintId} onFile={onFile} />
        {value && (
          <button type="button" className={buttonClass('secondary', 'sm')} disabled={busy} onClick={onRemove}>
            {text.remove}
          </button>
        )}
        <UploadNotes status={status} warning={warning} problem={problem} />
      </div>
    </details>
  );
}
