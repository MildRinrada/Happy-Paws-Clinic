/* eslint-disable @next/next/no-img-element -- the photo is a short-lived signed link from the API or a local preview of the picked file, not a file next/image can optimise. */
import { publicProfileText } from '../../content/public-profile';

/** The photo as the form stands: the picture, or a note that there is none. */
export function ProfilePhotoPreview({ src, note }: { src?: string; note?: string }) {
  const text = publicProfileText.photo;

  if (!src) {
    return <p className="text-sm text-ink-600">{text.none}</p>;
  }

  return (
    <div className="space-y-1.5">
      <div className="relative aspect-square w-32 overflow-hidden rounded-lg border border-ink-200 bg-ink-50">
        <img src={src} alt={text.previewAlt} className="absolute inset-0 size-full object-cover" />
      </div>
      {note && <p className="text-xs text-ink-600">{note}</p>}
    </div>
  );
}
