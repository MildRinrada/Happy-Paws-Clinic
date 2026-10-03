'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { dataRequestAction } from '../../actions/privacy-actions';
import { portalAccount } from '../../content/portal-account';
import type { DataRequestType } from '../../types/privacy.types';
import { TextAreaField } from '../shared/text-area-field';
import { FormFooter, type FormStatus } from './form-footer';

const TYPES: DataRequestType[] = ['access', 'correction', 'portability', 'erasure'];

/** A PDPA request to the clinic: what the owner wants done with their data, and any detail. */
export function DataRequestForm() {
  const text = portalAccount.dataRequest;
  const [type, setType] = useState<DataRequestType>('access');
  const [detail, setDetail] = useState('');
  const [status, setStatus] = useState<FormStatus>({ kind: 'idle' });
  const [pending, startTransition] = useTransition();

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus({ kind: 'idle' });

    startTransition(async () => {
      try {
        const result = await dataRequestAction({ type, detail });

        if (result.ok) {
          setDetail('');
          setStatus({ kind: 'saved', message: text.done });
        } else {
          setStatus({ kind: 'error', message: result.error.message });
        }
      } catch {
        setStatus({ kind: 'error', message: portalAccount.unreachable });
      }
    });
  }

  return (
    <form onSubmit={submit} className="space-y-4">
      <label className="block space-y-1.5">
        <span className="block text-sm font-semibold text-ink-900">{text.type}</span>
        <select
          value={type}
          onChange={(event) => setType(event.target.value as DataRequestType)}
          className="h-11 w-full rounded-xl border border-ink-200 bg-white px-3.5 text-sm text-ink-900"
        >
          {TYPES.map((item) => (
            <option key={item} value={item}>
              {text.types[item]}
            </option>
          ))}
        </select>
      </label>

      <TextAreaField id="data-request-detail" label={text.detail} value={detail} onChange={(e) => setDetail(e.target.value)} maxLength={2000} rows={3} />

      <FormFooter pending={pending} status={status} label={text.submit} />
    </form>
  );
}
