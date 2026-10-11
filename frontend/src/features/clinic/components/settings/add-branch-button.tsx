'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { clinicSettings } from '../../content/clinic-settings';
import { AddBranchForm } from './add-branch-form';

/** "+ เปิดสาขาใหม่" in the branches card's header, opening the form in a dialog. */
export function AddBranchButton() {
  const text = clinicSettings.branches;
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button variant="secondary" size="sm" onClick={() => setOpen(true)}>
        {text.add}
      </Button>
      <ModalDialog open={open} title={text.addTitle} onClose={() => setOpen(false)}>
        <AddBranchForm onDone={() => setOpen(false)} />
      </ModalDialog>
    </>
  );
}
