import { clinicLogoText } from '../../../content/clinic-logo-settings';
import { isUploadedImage } from '../../../lib/brand-image-src';

/** Says which logo the form will save: the paw, an upload, or a picture from a link. */
export function CurrentLogoNote({ value }: { value: string }) {
  const text = clinicLogoText;
  const which = !value ? text.usingPaw : isUploadedImage(value) ? text.usingUpload : text.usingLink;

  return (
    <p className="text-sm text-ink-700">
      {text.current}: <span className="font-medium">{which}</span>
    </p>
  );
}
