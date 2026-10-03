'use client';

import { useState, useTransition } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { removeContactAction, updateContactAction } from '../../actions/contact-actions';
import { contactSettings } from '../../content/contact-settings';
import type { ClinicContact, ContactInput } from '../../types/contact.types';
import { ContactForm } from './contact-form';

interface ContactRowProps {
  contact: ClinicContact;
  branches: { id: string; name: string }[];
}

const toInput = (contact: ClinicContact): ContactInput => ({
  kind: contact.kind,
  label: contact.label,
  value: contact.value,
  url: contact.url ?? '',
  embed_url: contact.embed_url ?? '',
  branch_id: contact.branch_id ?? '',
  sort_order: contact.sort_order,
});

/** One entry: what it says, and the buttons to change or remove it. Removing asks once more. */
export function ContactRow({ contact, branches }: ContactRowProps) {
  const text = contactSettings;
  const [mode, setMode] = useState<'view' | 'edit' | 'confirm'>('view');
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  const branchName = contact.branch_id ? (branches.find((branch) => branch.id === contact.branch_id)?.name ?? contact.branch_id) : text.clinicWide;

  if (mode === 'edit') {
    return (
      <li className="px-4 py-3">
        <ContactForm
          idPrefix={`contact-${contact.id}`}
          initial={toInput(contact)}
          branches={branches}
          submitLabel={text.save}
          onSubmit={(values) => updateContactAction({ id: contact.id, version: contact.version, values })}
          onDone={() => setMode('view')}
          onCancel={() => setMode('view')}
        />
      </li>
    );
  }

  function remove() {
    setError(undefined);
    startTransition(async () => {
      const result = await removeContactAction({ id: contact.id, version: contact.version });
      if (!result.ok) {
        setError(result.error.code === 'version_conflict' ? text.conflict : result.error.message);
        setMode('view');
      }
    });
  }

  return (
    <li className="flex flex-wrap items-center gap-x-4 gap-y-2 px-4 py-3">
      <span className="w-12 shrink-0 text-sm tabular-nums text-ink-500">{contact.sort_order}</span>
      <div className="min-w-0 flex-1">
        <p className="flex flex-wrap items-center gap-2">
          <Badge tone="brand">{text.kinds[contact.kind].name}</Badge>
          <span className="text-sm text-ink-600">{contact.label}</span>
          {contact.embed_url && <Badge tone="green">{text.hasEmbed}</Badge>}
        </p>
        <p className="mt-0.5 truncate font-medium text-ink-900">{contact.value}</p>
        {contact.url && <p className="truncate text-xs text-ink-500">{contact.url}</p>}
        {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
      </div>
      <span className="text-sm text-ink-600">{branchName}</span>

      <div className="flex gap-2">
        {mode === 'confirm' ? (
          <>
            <Button variant="ghost" size="sm" disabled={pending} onClick={() => setMode('view')}>
              {text.cancel}
            </Button>
            <Button variant="danger" size="sm" disabled={pending} onClick={remove}>
              {pending ? text.removing : text.confirmRemove}
            </Button>
          </>
        ) : (
          <>
            <Button variant="secondary" size="sm" onClick={() => setMode('edit')}>
              {text.edit}
            </Button>
            <Button variant="danger" size="sm" onClick={() => setMode('confirm')}>
              {text.remove}
            </Button>
          </>
        )}
      </div>
    </li>
  );
}
