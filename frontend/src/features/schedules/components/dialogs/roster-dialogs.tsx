'use client';

import { useState, type ReactNode } from 'react';
import { schedulePage } from '../../content/schedule-page';
import { RosterDialog } from './roster-dialog';
import { RosterDialogBody, type RosterDialogSetup } from './roster-dialog-body';
import { RosterDialogsContext, type RosterDialogRequest } from './roster-dialogs-context';

interface RosterDialogsProps extends RosterDialogSetup {
  children: ReactNode;
}

const TITLES: Record<RosterDialogRequest['kind'], string> = schedulePage.dialogs;

/**
 * Holds the roster's one dialog for a session with `schedule:write`: whatever on the page asks
 * (an empty cell, a shift chip, a header button) says what it wants, and this opens it with the
 * person and day already chosen.
 */
export function RosterDialogs({ children, ...setup }: RosterDialogsProps) {
  const [request, setRequest] = useState<RosterDialogRequest | null>(null);
  const close = () => setRequest(null);

  return (
    <RosterDialogsContext.Provider value={setRequest}>
      {children}
      <RosterDialog open={request !== null} title={request ? TITLES[request.kind] : ''} onClose={close}>
        {request && <RosterDialogBody request={request} setup={setup} onDone={close} />}
      </RosterDialog>
    </RosterDialogsContext.Provider>
  );
}
