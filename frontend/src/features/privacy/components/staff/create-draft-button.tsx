'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formMessages } from '@/features/settings/content/form-messages';
import { createPolicyDraftAction } from '../../actions/manage-privacy-policy-actions';
import { createDraftText } from '../../content/staff-privacy-policy';
import { policySettingsPaths } from '../../lib/privacy-paths';
import type { DraftSource } from '../../types/staff-privacy-policy.types';

interface CreateDraftButtonProps {
  source: DraftSource;
  label: string;
  variant?: 'primary' | 'secondary';
}

/** Makes a draft from the template, the current notice or a version, and opens it in the editor. */
export function CreateDraftButton({ source, label, variant = 'secondary' }: CreateDraftButtonProps) {
  const router = useRouter();
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function create() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await createPolicyDraftAction(source);
        if (result.ok) router.push(policySettingsPaths.version(result.data.id));
        else setError(result.error.message);
      } catch {
        setError(formMessages.unreachable);
      }
    });
  }

  return (
    <span className="flex flex-col items-start gap-1">
      <Button variant={variant} size="sm" disabled={pending} onClick={create}>
        {pending ? createDraftText.creating : label}
      </Button>
      {error && (
        <span role="alert" className="text-xs text-red-700">
          {error}
        </span>
      )}
    </span>
  );
}
