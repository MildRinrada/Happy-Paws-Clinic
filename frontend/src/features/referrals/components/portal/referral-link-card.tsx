'use client';

import { useId } from 'react';
import { SectionCard } from '@/features/portal/components/shared/section-card';
import { portalReferral } from '../../content/portal-referral';
import type { MyReferral } from '../../types/referral.types';
import { CopyLinkButton } from './copy-link-button';
import { LineShareLink } from './line-share-link';

/**
 * The link to share, readable and selectable on a phone, with copy and LINE buttons. The field is
 * read-only so the whole link can be selected by hand where copying is refused.
 */
export function ReferralLinkCard({ referral }: { referral: MyReferral }) {
  const text = portalReferral;
  const id = useId();

  return (
    <SectionCard title={text.linkHeading}>
      <div className="space-y-3">
        <label htmlFor={id} className="sr-only">
          {text.linkLabel}
        </label>
        <input
          id={id}
          readOnly
          value={referral.link}
          className="h-11 w-full min-w-0 rounded-xl border border-ink-200 bg-site-soft px-3 font-mono text-sm text-ink-900"
        />
        <p className="text-sm text-site-muted">{text.codeLabel(referral.code)}</p>
        <div className="flex flex-wrap gap-3">
          <CopyLinkButton link={referral.link} />
          <LineShareLink link={referral.link} />
        </div>
      </div>
    </SectionCard>
  );
}
