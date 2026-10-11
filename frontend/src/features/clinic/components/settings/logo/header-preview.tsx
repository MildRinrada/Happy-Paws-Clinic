import { clinicLogoText } from '../../../content/clinic-logo-settings';
import { PreviewMark } from './preview-mark';

interface HeaderPreviewProps {
  logoSrc?: string;
  /** Draw the name beside the mark, as the header would with this setting. */
  withName: boolean;
  /** Marks the variant the form will save. */
  selected: boolean;
}

/** The public header's left end at its desktop size, with the same limits `SiteLogo` keeps. */
export function HeaderPreview({ logoSrc, withName, selected }: HeaderPreviewProps) {
  const text = clinicLogoText.preview;
  const nameShown = withName || !logoSrc;

  return (
    <figure className="min-w-0 space-y-1.5">
      <figcaption className="flex items-center gap-2 text-xs text-ink-600">
        {withName ? text.withName : text.withoutName}
        {selected && <span className="rounded-full bg-brand-50 px-2 py-0.5 font-medium text-brand-700">{text.selected}</span>}
      </figcaption>
      <div className={`flex h-16 items-center overflow-hidden rounded-lg border bg-white px-5 ${selected ? 'border-brand-400' : 'border-ink-200'}`}>
        <div className="flex shrink-0 items-center gap-2.5">
          <PreviewMark
            src={logoSrc}
            markClassName="size-10"
            imageClassName={nameShown ? 'h-10 max-w-[160px]' : 'h-10 max-w-[240px]'}
          />
          {nameShown && <span className="whitespace-nowrap font-wordmark text-lg font-bold tracking-tight text-ink-900">{text.headerWordmark}</span>}
        </div>
      </div>
    </figure>
  );
}
