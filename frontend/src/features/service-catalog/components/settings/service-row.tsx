'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { serviceSettings } from '../../content/service-settings';
import type { ManagedService, ServiceCategory } from '../../types/managed-service.types';
import { ServiceAvailabilityForm } from './service-availability-form';
import { ServiceDetailsForm } from './service-details-form';
import { ServicePricingForm } from './service-pricing-form';
import { ServiceSummary } from './service-summary';

export interface ServiceRowProps {
  service: ManagedService;
  categories: ServiceCategory[];
  staff: Record<string, string> | null;
  branches: Record<string, string>;
  today: string;
}

type Mode = 'view' | 'details' | 'pricing' | 'availability';

/** One service, and the three things that can be changed about it, one open at a time. */
export function ServiceRow({ service, categories, staff, branches, today }: ServiceRowProps) {
  const text = serviceSettings.actions;
  const [mode, setMode] = useState<Mode>('view');
  const close = () => setMode('view');

  return (
    <li className="space-y-3 px-5 py-4">
      <div className="flex flex-wrap items-start gap-3">
        <ServiceSummary service={service} />
        <div className="flex flex-wrap gap-2">
          <Button variant={mode === 'details' ? 'primary' : 'secondary'} size="sm" onClick={() => setMode(mode === 'details' ? 'view' : 'details')}>
            {text.details}
          </Button>
          <Button variant={mode === 'pricing' ? 'primary' : 'secondary'} size="sm" onClick={() => setMode(mode === 'pricing' ? 'view' : 'pricing')}>
            {text.pricing}
          </Button>
          <Button
            variant={mode === 'availability' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setMode(mode === 'availability' ? 'view' : 'availability')}
          >
            {text.availability}
          </Button>
        </div>
      </div>

      {mode === 'details' && <ServiceDetailsForm service={service} categories={categories} staff={staff} onDone={close} />}
      {mode === 'pricing' && <ServicePricingForm service={service} today={today} onDone={close} />}
      {mode === 'availability' && <ServiceAvailabilityForm service={service} branches={branches} onDone={close} />}
    </li>
  );
}
