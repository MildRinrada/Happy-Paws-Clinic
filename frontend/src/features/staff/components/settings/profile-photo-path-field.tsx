import { useId } from 'react';
import { Field, Input } from '@/components/ui/field';
import { publicProfileText } from '../../content/public-profile';

interface ProfilePhotoPathFieldProps {
  /** The site path, or empty while the photo is an uploaded one. */
  value: string;
  onChange: (path: string) => void;
}

/**
 * The § 10 way of setting a photo, kept for files already under the site's /public: tucked under
 * "ขั้นสูง" because uploading is the usual way now.
 */
export function ProfilePhotoPathField({ value, onChange }: ProfilePhotoPathFieldProps) {
  const text = publicProfileText.photo;
  const id = useId();

  return (
    <details className="text-sm" open={value !== '' || undefined}>
      <summary className="cursor-pointer text-ink-600">{text.advanced}</summary>
      <div className="mt-3">
        <Field label={text.pathLabel} htmlFor={`${id}-path`} hint={text.pathHint}>
          <Input id={`${id}-path`} value={value} maxLength={300} placeholder="/images/team/mint.jpg" onChange={(event) => onChange(event.target.value)} />
        </Field>
      </div>
    </details>
  );
}
