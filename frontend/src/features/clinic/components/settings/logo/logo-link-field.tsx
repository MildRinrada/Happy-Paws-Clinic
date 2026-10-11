import { useId } from 'react';
import { Field, Input } from '@/components/ui/field';
import { clinicLogoText } from '../../../content/clinic-logo-settings';

interface LogoLinkFieldProps {
  /** The `https://` address, or empty while the logo is an uploaded one or the paw. */
  value: string;
  onChange: (url: string) => void;
  error?: string;
}

/**
 * The way a logo was set before § 50, an address of a picture hosted elsewhere, kept for clinics
 * that already use one: tucked under "ขั้นสูง" because uploading is the usual way now.
 */
export function LogoLinkField({ value, onChange, error }: LogoLinkFieldProps) {
  const text = clinicLogoText;
  const id = useId();

  return (
    <details className="text-sm" open={value !== '' || undefined}>
      <summary className="cursor-pointer text-ink-600">{text.advanced}</summary>
      <div className="mt-3">
        <Field label={text.linkLabel} htmlFor={`${id}-link`} hint={text.linkHint} error={error}>
          <Input id={`${id}-link`} type="url" value={value} maxLength={1000} placeholder="https://" onChange={(event) => onChange(event.target.value)} />
        </Field>
      </div>
    </details>
  );
}
