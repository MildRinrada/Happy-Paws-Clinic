'use client';

import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { TimeStep, type PickRefusal } from '@/features/booking/components/time-step';
import { suggestedTimes } from '@/features/booking/lib/slot-times';
import { clinicDay } from '@/lib/format/datetime';
import { rescheduleAction } from '../../actions/appointment-actions';
import { portalReschedule } from '../../content/portal-reschedule';
import { portalPaths } from '../../content/portal-navigation';
import type { RescheduleView } from '../../lib/load-reschedule';
import { RescheduleConfirm, type RescheduleRefusal } from './reschedule-confirm';
import { RescheduleDone } from './reschedule-done';

interface Picked {
  startsAt: string;
  staffId?: string;
}

/**
 * Choosing the new time with the booking page's own slot picker, then confirming the move.
 *
 * Unlike booking, nothing is held while the owner reads the summary: the reschedule endpoint takes
 * no hold and checks the time again itself. A time taken meanwhile comes back as
 * `slot_unavailable` with nearby times, and the picker shows them.
 *
 * The vet: the one the owner filtered to, else the appointment's own vet when free at that time,
 * else the first vet free then. Sending none would make the backend keep the old vet even when the
 * time was offered only because someone else is free.
 */
export function RescheduleWizard({ appointment, vetNames }: { appointment: RescheduleView; vetNames: Record<string, string> }) {
  const router = useRouter();
  const [picked, setPicked] = useState<Picked>();
  // The day last picked from, so the picker reopens there (and its suggested times mean that day).
  const [day, setDay] = useState(appointment.day);
  const [refusal, setRefusal] = useState<PickRefusal>();
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string>();
  const [refused, setRefused] = useState<RescheduleRefusal>();
  const [movedTo, setMovedTo] = useState<string>();
  const text = portalReschedule;

  function pick(startsAt: string, vet?: string, freeStaffIds?: string[]) {
    const own = appointment.staffId && freeStaffIds?.includes(appointment.staffId) ? appointment.staffId : undefined;

    setPicked({ startsAt, staffId: vet ?? own ?? freeStaffIds?.[0] });
    setDay(clinicDay(startsAt));
    setRefusal(undefined);
    setError(undefined);
  }

  async function confirm() {
    if (!picked) return;
    setSubmitting(true);
    setError(undefined);

    try {
      const result = await rescheduleAction({
        appointmentId: appointment.id,
        version: appointment.version,
        startsAt: picked.startsAt,
        staffId: picked.staffId,
      });

      if (result.ok) {
        setMovedTo(result.data.starts_at);
      } else if (result.error.code === 'slot_unavailable') {
        // Taken after all: back to the times, with the alternatives the backend offered.
        setRefusal({ message: result.error.message, suggested: suggestedTimes(result.error.details) });
        setPicked(undefined);
      } else if (result.error.code === 'cutoff_passed') {
        const closedAt = result.error.details?.cutoff_at;
        setRefused({ message: result.error.message, closedAt: typeof closedAt === 'string' ? closedAt : undefined });
      } else if (result.error.code === 'version_conflict') {
        setError(text.versionConflict);
      } else {
        setError(result.error.message);
      }
    } catch {
      setError(text.unreachable);
    } finally {
      setSubmitting(false);
    }
  }

  if (movedTo) {
    return <RescheduleDone startsAt={movedTo} />;
  }

  if (picked) {
    return (
      <RescheduleConfirm
        fromAt={appointment.startsAt}
        toAt={picked.startsAt}
        vet={(picked.staffId && vetNames[picked.staffId]) || text.vetFallback}
        submitting={submitting}
        error={error}
        refused={refused}
        onConfirm={confirm}
        onBack={() => setPicked(undefined)}
      />
    );
  }

  return (
    <TimeStep
      serviceId={appointment.serviceId}
      branchId={appointment.branchId}
      vetNames={vetNames}
      onPick={pick}
      holding={false}
      refusal={refusal}
      onBack={() => router.push(portalPaths.appointments)}
      initialDay={day}
    />
  );
}
