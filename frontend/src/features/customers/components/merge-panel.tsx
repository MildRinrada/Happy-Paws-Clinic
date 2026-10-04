'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { mergeCustomer } from '../content/merge-customer';
import type { Customer, MergeResult } from '../types/customer.types';
import { MergeConfirmation } from './merge-confirmation';
import { MergeResultNotice } from './merge-result-notice';
import { MergeSearch } from './merge-search';

type Step = { name: 'closed' } | { name: 'search' } | { name: 'confirm'; duplicate: Customer } | { name: 'done'; result: MergeResult };

/** The merge in three steps: find the duplicate, read what will happen, then merge. */
export function MergePanel({ customer }: { customer: Customer }) {
  const [step, setStep] = useState<Step>({ name: 'closed' });

  switch (step.name) {
    case 'closed':
      return (
        <Button variant="secondary" size="sm" onClick={() => setStep({ name: 'search' })}>
          {mergeCustomer.start}
        </Button>
      );
    case 'search':
      return (
        <MergeSearch
          customerId={customer.id}
          onPick={(duplicate) => setStep({ name: 'confirm', duplicate })}
          onCancel={() => setStep({ name: 'closed' })}
        />
      );
    case 'confirm':
      return (
        <MergeConfirmation
          surviving={customer}
          duplicate={step.duplicate}
          onBack={() => setStep({ name: 'search' })}
          onDone={(result) => setStep({ name: 'done', result })}
        />
      );
    case 'done':
      return <MergeResultNotice result={step.result} />;
  }
}
