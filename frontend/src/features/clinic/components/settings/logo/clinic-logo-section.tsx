'use client';

import { useId, useState } from 'react';
import { buttonClass } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { clinicLogoText } from '../../../content/clinic-logo-settings';
import { useBrandImageUpload } from '../../../hooks/use-brand-image-upload';
import { isUploadedImage, logoSrcOf, tabIconSrcOf } from '../../../lib/brand-image-src';
import { logoWarning, tabIconWarning } from '../../../lib/brand-image-warnings';
import type { ClinicProfileInput } from '../../../types/settings.types';
import { CurrentLogoNote } from './current-logo-note';
import { ImageDropZone } from './image-drop-zone';
import { LogoLinkField } from './logo-link-field';
import { LogoPreviews } from './logo-previews';
import { TabIconDetails } from './tab-icon-details';
import { UploadNotes } from './upload-notes';

/** The parts of the profile this section edits, and the name its previews show. */
type ClinicLogoValues = Pick<ClinicProfileInput, 'name' | 'logo_url' | 'tab_icon_url' | 'show_name_with_logo'>;

interface ClinicLogoSectionProps {
  values: ClinicLogoValues;
  onChange: <K extends keyof ClinicProfileInput>(field: K, value: ClinicProfileInput[K]) => void;
  errors: Partial<Record<string, string>>;
}

/**
 * The clinic's own logo in place of the paw (docs/contract-extensions.md § 50), whether its name is
 * written beside it, and an optional square icon for the browser tab. The card around it carries
 * the heading. The name switch is offered only once there is a logo: beside the paw the name is
 * always written, so the switch would change nothing.
 *
 * A chosen file goes to the storage at once, but only the form's value changes; the website
 * changes when the form is saved, the way a staff photo does (§ 32).
 */
export function ClinicLogoSection({ values, onChange, errors }: ClinicLogoSectionProps) {
  const text = clinicLogoText;
  const hintId = useId();
  const [logoStatus, setLogoStatus] = useState<string>();
  const [tabStatus, setTabStatus] = useState<string>();

  const logo = useBrandImageUpload('logo', (value) => {
    onChange('logo_url', value);
    setLogoStatus(text.uploaded);
  });
  const tab = useBrandImageUpload('tab-icon', (value) => {
    onChange('tab_icon_url', value);
    setTabStatus(text.uploaded);
  });

  // The file just picked is shown from memory; anything else from where the website will load it.
  const logoPicked = logo.picked?.value === values.logo_url ? logo.picked : undefined;
  const tabPicked = tab.picked?.value === values.tab_icon_url ? tab.picked : undefined;
  const logoSrc = logoPicked?.preview ?? logoSrcOf(values.logo_url);
  const tabIconSrc = tabPicked?.preview ?? tabIconSrcOf(values.tab_icon_url);

  function pickLogo(file: File) {
    setLogoStatus(undefined);
    void logo.upload(file);
  }

  function pickTabIcon(file: File) {
    setTabStatus(undefined);
    void tab.upload(file);
  }

  function backToPaw() {
    logo.clearProblem();
    onChange('logo_url', '');
    setLogoStatus(text.removed);
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-ink-600">{text.intro}</p>

      <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.4fr)]">
        <div className="space-y-3">
          <CurrentLogoNote value={values.logo_url} />
          <p id={hintId} className="text-xs text-ink-600">
            {text.hint}
          </p>
          <ImageDropZone label={text.heading} busy={logo.busy} hasImage={Boolean(values.logo_url)} hintId={hintId} onFile={pickLogo} />
          {values.logo_url && (
            <button type="button" className={buttonClass('secondary', 'sm')} disabled={logo.busy} onClick={backToPaw}>
              {text.usePaw}
            </button>
          )}
          <UploadNotes status={logoStatus} warning={logoWarning(logoPicked)} problem={logo.problem ?? errors.logo_url} />

          {values.logo_url && (
            <Checkbox
              label={text.showName}
              hint={text.showNameHint}
              checked={values.show_name_with_logo}
              onChange={(event) => onChange('show_name_with_logo', event.target.checked)}
            />
          )}

          <LogoLinkField
            value={isUploadedImage(values.logo_url) ? '' : values.logo_url}
            onChange={(url) => onChange('logo_url', url)}
          />

          <TabIconDetails
            value={values.tab_icon_url}
            src={tabIconSrc}
            busy={tab.busy}
            status={tabStatus}
            warning={tabIconWarning(tabPicked)}
            problem={tab.problem ?? errors.tab_icon_url}
            onFile={pickTabIcon}
            onRemove={() => {
              tab.clearProblem();
              onChange('tab_icon_url', '');
              setTabStatus(text.tabIcon.removed);
            }}
          />
        </div>

        <LogoPreviews logoSrc={logoSrc} tabIconSrc={tabIconSrc} name={values.name} showName={values.show_name_with_logo} />
      </div>
    </div>
  );
}
