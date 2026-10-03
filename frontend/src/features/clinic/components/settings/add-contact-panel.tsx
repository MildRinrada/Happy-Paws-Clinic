'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { addContactAction } from '../../actions/contact-actions';
import { contactSettings } from '../../content/contact-settings';
import { ContactForm } from './contact-form';

interface AddContactPanelProps {
  branches: { id: string; name: string }[];
  /** Placed after the last entry unless the clinic says otherwise. */
  nextOrder: number;
}

/** The button that opens a blank entry form at the foot of the list. */
export function AddContactPanel({ branches, nextOrder }: AddContactPanelProps) {
  const [open, setOpen] = useState(false);

  if (!open) {
    return (
      <Button variant="secondary" onClick={() => setOpen(true)}>
        {contactSettings.add}
      </Button>
    );
  }

  return (
    <ContactForm
      idPrefix="new-contact"
      initial={{ kind: 'phone', label: '', value: '', url: '', embed_url: '', branch_id: '', sort_order: nextOrder }}
      branches={branches}
      submitLabel={contactSettings.save}
      onSubmit={addContactAction}
      onDone={() => setOpen(false)}
      onCancel={() => setOpen(false)}
    />
  );
}
