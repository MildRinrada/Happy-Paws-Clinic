'use client';

import { useState } from 'react';
import type { Me } from '@/features/account/types/account.types';
import { portalAccount } from '../../content/portal-account';
import { AccountSection } from './account-section';
import { ChannelsForm } from './channels-form';
import { ProfileForm } from './profile-form';

/**
 * The two forms that both write `PATCH /me`. They share the account's version: each save moves it
 * on, and the other form must send the new one or the backend would refuse it as a stale edit.
 */
export function ContactSettings({ me }: { me: Me }) {
  const [version, setVersion] = useState(me.version);
  const onSaved = (next?: number) => next !== undefined && setVersion(next);

  return (
    <>
      <AccountSection title={portalAccount.profile.title}>
        <ProfileForm me={me} version={version} onSaved={onSaved} />
      </AccountSection>

      <AccountSection title={portalAccount.channels.title} intro={portalAccount.channels.intro}>
        <ChannelsForm
          initial={me.notifications ?? { email: false, sms: false, line: false }}
          version={version}
          onSaved={onSaved}
        />
      </AccountSection>
    </>
  );
}
