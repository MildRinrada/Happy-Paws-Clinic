'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createCouponAction } from '../../actions/crm-actions';
import { couponPage } from '../../content/coupon-page';
import { crmArea } from '../../content/crm-tabs';
import { couponProblems, emptyCouponDraft, toCouponInput, type CouponDraft, type CouponProblem } from '../../lib/coupon-draft';
import type { BranchOption, Coupon, ServiceOption } from '../../types/crm.types';
import { BranchSelect } from '../shared/branch-select';
import { ServiceChecklist } from '../shared/service-checklist';
import { CouponLimitFields } from './coupon-limit-fields';
import { CouponPeriodFields } from './coupon-period-fields';
import { CouponResult } from './coupon-result';
import { CouponValueFields } from './coupon-value-fields';

interface Props {
  services: ServiceOption[];
  branches: BranchOption[];
  /** The branch picked first. '' (every branch) unless the session is bound to its branches. */
  defaultBranchId: string;
  /** False when the session is bound to its branches and must name one of them. */
  allowAllBranches: boolean;
  today: string;
}

/** `POST /staff/coupons`: a code, how much it takes off, on what, how often, and when. */
export function CouponForm({ services, branches, defaultBranchId, allowAllBranches, today }: Props) {
  const text = couponPage;
  const [draft, setDraft] = useState<CouponDraft>(() => emptyCouponDraft(today, defaultBranchId));
  const [tried, setTried] = useState(false);
  const [coupon, setCoupon] = useState<Coupon>();
  const { errors, message, pending, submit, touch } = useFormSubmit({ conflict: crmArea.conflict, unreachable: crmArea.unreachable });
  const problems = couponProblems(draft);
  const show = (problem: CouponProblem) => (tried && problems.includes(problem) ? text.errors[problem] : undefined);

  function change(patch: Partial<CouponDraft>) {
    touch();
    setDraft((current) => ({ ...current, ...patch }));
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setTried(true);
    if (problems.length > 0) return;

    submit(() => createCouponAction(toCouponInput(draft)), setCoupon);
  }

  function another() {
    setCoupon(undefined);
    setDraft(emptyCouponDraft(today, defaultBranchId));
    setTried(false);
  }

  if (coupon) {
    return <CouponResult coupon={coupon} branches={branches} onAnother={another} />;
  }

  return (
    <form onSubmit={save} className="space-y-5" noValidate>
      <div className="grid gap-3 sm:grid-cols-[14rem_1fr]">
        <Field label={text.code} htmlFor="coupon-code" error={errors.code ?? show('code')} hint={text.codeHint}>
          <Input
            id="coupon-code"
            value={draft.code}
            maxLength={20}
            placeholder={text.codePlaceholder}
            autoComplete="off"
            className="font-mono uppercase"
            onChange={(event) => change({ code: event.target.value.toUpperCase() })}
          />
        </Field>
        <Field label={text.name} htmlFor="coupon-name" error={errors.name ?? show('name')}>
          <Input
            id="coupon-name"
            value={draft.name}
            maxLength={120}
            placeholder={text.namePlaceholder}
            onChange={(event) => change({ name: event.target.value })}
          />
        </Field>
      </div>

      <CouponValueFields
        valueType={draft.valueType}
        value={draft.value}
        error={errors.percent ?? errors.amount_satang ?? show(draft.valueType)}
        onType={(valueType) => change({ valueType })}
        onValue={(value) => change({ value })}
      />

      <ServiceChecklist
        legend={text.services}
        hint={text.servicesHint}
        emptyText={text.noServices}
        error={errors.service_ids}
        services={services}
        selected={draft.serviceIds}
        onChange={(serviceIds) => change({ serviceIds })}
      />

      <CouponLimitFields
        maxRedemptions={draft.maxRedemptions}
        perCustomerLimit={draft.perCustomerLimit}
        errors={{
          maxRedemptions: errors.max_redemptions ?? show('maxRedemptions'),
          perCustomerLimit: errors.per_customer_limit ?? show('perCustomerLimit'),
        }}
        onChange={change}
      />

      <CouponPeriodFields
        validFrom={draft.validFrom}
        validUntil={draft.validUntil}
        errors={{ validFrom: errors.valid_from, validUntil: errors.valid_until ?? show('validUntil') }}
        onChange={change}
      />

      {(branches.length > 1 || !allowAllBranches) && (
        <BranchSelect
          id="coupon-branch"
          label={text.branch}
          allLabel={allowAllBranches ? text.allBranches : undefined}
          error={errors.branch_id}
          branches={branches}
          value={draft.branchId}
          onChange={(branchId) => change({ branchId })}
        />
      )}

      <div className="flex flex-wrap items-center gap-3">
        <Button type="submit" disabled={pending}>
          {pending ? text.saving : text.submit}
        </Button>
        <FormStatus message={message} savedText="" />
      </div>
    </form>
  );
}
