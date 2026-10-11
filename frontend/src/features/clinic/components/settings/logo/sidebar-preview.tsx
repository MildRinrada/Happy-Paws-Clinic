import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import { clinicLogoText } from '../../../content/clinic-logo-settings';
import { PreviewMark } from './preview-mark';

interface SidebarPreviewProps {
  logoSrc?: string;
  name: string;
  showName: boolean;
}

/** The top of the staff menu at its real width, with the same limits `ClinicBrandMark` keeps. */
export function SidebarPreview({ logoSrc, name, showName }: SidebarPreviewProps) {
  const nameShown = showName || !logoSrc;

  return (
    <figure className="space-y-1.5">
      <figcaption className="text-xs text-ink-600">{clinicLogoText.preview.sidebar}</figcaption>
      <div className="w-60 max-w-full rounded-lg border border-ink-200 bg-white px-4 pb-4 pt-5">
        <div className="flex min-w-0 items-center gap-2.5">
          <PreviewMark src={logoSrc} markClassName="size-9 shrink-0" imageClassName={nameShown ? 'h-9 max-w-[96px]' : 'h-9 max-w-[200px]'} />
          {nameShown && (
            <div className="min-w-0">
              <p className="truncate font-display text-base font-semibold leading-tight text-brand-800">{name || staffShellText.brandFallback}</p>
              <p className="truncate text-xs text-ink-500">{staffShellText.brandTagline}</p>
            </div>
          )}
        </div>
        <div className="mt-4 space-y-2" aria-hidden="true">
          <div className="h-8 rounded-lg bg-brand-50" />
          <div className="h-8 rounded-lg bg-ink-50" />
        </div>
      </div>
    </figure>
  );
}
