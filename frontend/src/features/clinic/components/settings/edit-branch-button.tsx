'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { ModalDialog } from '@/components/ui/modal-dialog';
import { clinicSettings } from '../../content/clinic-settings';
import type { Branch } from '../../types/clinic.types';
import { EditBranchForm } from './edit-branch-form';

/** "แก้ไข" on one branch, opening its edit form in a dialog. */
export function EditBranchButton({ branch }: { branch: Branch }) {
  const text = clinicSettings.branches;
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button variant="secondary" size="sm" aria-label={text.editTitle(branch.name)} onClick={() => setOpen(true)}>
        {text.edit}
      </Button>
      <ModalDialog open={open} title={text.editTitle(branch.name)} onClose={() => setOpen(false)}>
        <EditBranchForm branch={branch} onDone={() => setOpen(false)} />
      </ModalDialog>
    </>
  );
}
