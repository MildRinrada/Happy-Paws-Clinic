'use client';

import { useId, useState } from 'react';
import { clinicLogoText } from '../../../content/clinic-logo-settings';
import { BrowserTabPreview } from './browser-tab-preview';
import { HeaderPreview } from './header-preview';
import { SidebarPreview } from './sidebar-preview';

interface LogoPreviewsProps {
  logoSrc?: string;
  tabIconSrc?: string;
  name: string;
  showName: boolean;
}

/**
 * How the form's choices will look before they are saved, drawn from the form, not the saved
 * profile. At first only the public header as the form would save it; "ดูตัวอย่างทั้งหมด" adds the
 * header the other way, the staff menu and the browser tab.
 */
export function LogoPreviews({ logoSrc, tabIconSrc, name, showName }: LogoPreviewsProps) {
  const text = clinicLogoText.preview;
  const moreId = useId();
  const [all, setAll] = useState(false);
  // Beside the paw the name is always written, whatever the switch says.
  const withName = showName || !logoSrc;

  return (
    <section aria-label={text.heading} className="min-w-0 space-y-3 rounded-xl bg-ink-50 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-sm font-medium text-ink-700">{text.heading}</h3>
        <button
          type="button"
          aria-expanded={all}
          aria-controls={moreId}
          className="text-sm font-medium text-brand-700 hover:underline"
          onClick={() => setAll(!all)}
        >
          {all ? text.showSelected : text.showAll}
        </button>
      </div>

      <p className="text-xs font-medium text-ink-500">{text.header}</p>
      <HeaderPreview logoSrc={logoSrc} withName={withName} selected={all} />

      <div id={moreId} hidden={!all} className="space-y-3">
        {logoSrc && <HeaderPreview logoSrc={logoSrc} withName={!withName} selected={false} />}
        <div className="flex flex-wrap items-start gap-4">
          <SidebarPreview logoSrc={logoSrc} name={name} showName={showName} />
          <BrowserTabPreview logoSrc={logoSrc} tabIconSrc={tabIconSrc} name={name} />
        </div>
      </div>
    </section>
  );
}
