/* eslint-disable @next/next/no-img-element -- a local preview of the chosen file, or the API's redirect to a signed link. */
import { LogoMark } from '@/components/layout/site-header/logo-mark';

interface PreviewMarkProps {
  src?: string;
  /** The paw's box in the place being shown. */
  markClassName: string;
  /** The logo's height and widest width in that place. */
  imageClassName: string;
}

/** The mark as a place on the site draws it: the logo, or the paw when there is none. */
export function PreviewMark({ src, markClassName, imageClassName }: PreviewMarkProps) {
  if (!src) return <LogoMark className={markClassName} />;

  return <img src={src} alt="" className={`w-auto shrink-0 rounded-lg object-contain ${imageClassName}`} />;
}
