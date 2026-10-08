'use client';

import { useEffect, useState } from 'react';
import { Alert } from '@/components/ui/alert';
import { loadSlotsAction } from '../actions/booking-actions';
import { bookingPage } from '../content/booking-page';
import { clinicInstant, nextClinicDays } from '../lib/booking-dates';
import { slotTimes, vetsIn } from '../lib/slot-times';
import type { Slot } from '../types/booking.types';
import { DayPicker } from './day-picker';
import { SlotGrid } from './slot-grid';
import { SuggestedTimes } from './suggested-times';
import { VetFilter } from './vet-filter';

export interface PickRefusal {
  message: string;
  /** "HH:mm" times the backend offered instead, on the same day. */
  suggested: string[];
}

interface TimeStepProps {
  serviceId: string;
  branchId: string;
  vetNames: Record<string, string>;
  /**
   * Hold this time. Without a vet, the backend picks whoever is free. `freeStaffIds` lists every vet
   * free at that time when it is known (a time from the grid), for a caller that picks the vet itself.
   */
  onPick: (startsAt: string, staffId?: string, freeStaffIds?: string[]) => void;
  holding: boolean;
  refusal?: PickRefusal;
  onBack: () => void;
  /** The clinic day to open on (`YYYY-MM-DD`). Today when left out. */
  initialDay?: string;
}

type SlotState = { status: 'loading' } | { status: 'ready'; slots: Slot[] } | { status: 'error'; message: string };

/** Step 2: a day, optionally a vet, and one of the free times. */
export function TimeStep({ serviceId, branchId, vetNames, onPick, holding, refusal, onBack, initialDay }: TimeStepProps) {
  const text = bookingPage.time;
  const [day, setDay] = useState(() => initialDay ?? nextClinicDays(1)[0]!);
  const [vet, setVet] = useState('');
  const [state, setState] = useState<SlotState>({ status: 'loading' });

  useEffect(() => {
    let current = true;
    setState({ status: 'loading' });

    loadSlotsAction({ serviceId, branchId, date: day })
      .then((result) => {
        if (!current) return;
        setState(result.ok ? { status: 'ready', slots: result.data.slots } : { status: 'error', message: result.error.message });
      })
      .catch(() => current && setState({ status: 'error', message: bookingPage.unreachable }));

    return () => {
      current = false;
    };
  }, [serviceId, branchId, day]);

  const slots = state.status === 'ready' ? state.slots : [];
  const vets = vetsIn(slots);
  const times = slotTimes(slots, vet || undefined);

  return (
    <div className="space-y-6">
      <section className="space-y-2">
        <h2 className="text-sm font-semibold text-ink-900">{text.day}</h2>
        <DayPicker
          value={day}
          onChange={(next) => {
            setDay(next);
            setVet('');
          }}
        />
      </section>

      {vets.length > 1 && <VetFilter vetIds={vets} names={vetNames} value={vet} onChange={setVet} />}

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-ink-900">{text.slots}</h2>

        {refusal && (
          <div className="space-y-2">
            <Alert>{refusal.message}</Alert>
            <SuggestedTimes
              times={refusal.suggested}
              disabled={holding}
              onPick={(time) => onPick(clinicInstant(day, time), vet || undefined)}
            />
          </div>
        )}

        {state.status === 'loading' && <p className="text-sm text-site-muted">{text.loading}</p>}
        {state.status === 'error' && <Alert>{state.message}</Alert>}
        {state.status === 'ready' && times.length === 0 && <p className="text-sm text-site-muted">{text.none}</p>}
        {times.length > 0 && <SlotGrid times={times} disabled={holding} onPick={(time) => onPick(time.startsAt, vet || undefined, time.staffIds)} />}
        {holding && <p className="text-sm text-site-muted">{text.holding}</p>}
      </section>

      <button type="button" onClick={onBack} className="text-sm font-semibold text-site-teal hover:underline">
        {text.back}
      </button>
    </div>
  );
}
