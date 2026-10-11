'use client';

import { useRef, useState, type ChangeEvent, type DragEvent } from 'react';
import { buttonClass } from '@/components/ui/button';
import { clinicLogoText } from '../../../content/clinic-logo-settings';

interface ImageDropZoneProps {
  /** Says which picture the button picks, for screen readers. */
  label: string;
  busy: boolean;
  /** "เปลี่ยนรูป" rather than "เลือกไฟล์" once there is a picture. */
  hasImage: boolean;
  /** The id of the line that explains the rules, read with the button. */
  hintId: string;
  onFile: (file: File) => void;
}

/** A box to drop a picture on, with the ordinary file button inside for anyone not dragging. */
export function ImageDropZone({ label, busy, hasImage, hintId, onFile }: ImageDropZoneProps) {
  const text = clinicLogoText;
  const input = useRef<HTMLInputElement>(null);
  const [over, setOver] = useState(false);

  function choose(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (file) onFile(file);
    event.target.value = '';
  }

  function drop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    setOver(false);
    const file = event.dataTransfer.files[0];
    if (file && !busy) onFile(file);
  }

  return (
    <div
      onDragOver={(event) => {
        event.preventDefault();
        setOver(true);
      }}
      onDragLeave={() => setOver(false)}
      onDrop={drop}
      className={`flex flex-wrap items-center justify-center gap-x-3 gap-y-2 rounded-xl border-2 border-dashed px-4 py-5 text-sm text-ink-600 transition-colors ${
        over ? 'border-brand-500 bg-brand-50' : 'border-ink-200 bg-ink-50/60'
      }`}
    >
      <span>{text.drop}</span>
      <span className="text-ink-400">{text.or}</span>
      <label className={buttonClass('secondary', 'sm', busy ? 'pointer-events-none opacity-60' : 'cursor-pointer')}>
        {busy ? text.uploading : hasImage ? text.change : text.choose}
        <input
          ref={input}
          type="file"
          accept="image/png,image/jpeg,image/webp"
          className="sr-only"
          aria-label={label}
          aria-describedby={hintId}
          disabled={busy}
          onChange={choose}
        />
      </label>
    </div>
  );
}
