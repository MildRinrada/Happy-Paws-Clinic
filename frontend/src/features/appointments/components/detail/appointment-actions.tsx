'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { clinicInstant } from '@/features/booking/lib/booking-dates';
import { OpenCaseButton } from '@/features/consultations/components/open-case-button';
import { suggestedTimes } from '@/features/booking/lib/slot-times';
import type { ActionResult } from '@/lib/actions/action-result';
import {
  cancelAppointmentAction,
  checkInAppointmentAction,
  confirmAppointmentAction,
  markNoShowAction,
  rescheduleAppointmentAction,
} from '../../actions/calendar-actions';
import { appointmentDetail } from '../../content/appointment-calendar';
import { refusalText } from '../../lib/refusal';
import { isOpen } from '../../lib/status-labels';
import type { Appointment } from '../../types/appointment.types';
import type { VetOption } from '../../types/calendar.types';
import type { PanelPermissions } from './appointment-panel';
import { CancelForm } from './cancel-form';
import { RescheduleForm } from './reschedule-form';

interface AppointmentActionsProps {
  appointment: Appointment;
  vets: VetOption[];
  permissions: PanelPermissions;
  isToday: boolean;
}

type Mode = 'idle' | 'cancel' | 'reschedule';

/**
 * What the counter can still do with this appointment. Only the actions that fit its state and
 * this session's scopes are offered; the backend checks both again and its refusal is shown here.
 */
export function AppointmentActions({ appointment, vets, permissions, isToday }: AppointmentActionsProps) {
  const text = appointmentDetail.actions;
  const [mode, setMode] = useState<Mode>('idle');
  const [error, setError] = useState<string>();
  const [notice, setNotice] = useState<string>();
  const [suggested, setSuggested] = useState<string[]>([]);
  const [checkInKey, setCheckInKey] = useState(() => crypto.randomUUID());
  const [pending, startTransition] = useTransition();

  const open = isOpen(appointment.status);
  const started = Date.parse(appointment.starts_at) <= Date.now();

  function run<T>(call: () => Promise<ActionResult<T>>, onDone?: (data: T) => void) {
    setError(undefined);
    setNotice(undefined);

    startTransition(async () => {
      const result = await call();

      if (result.ok) {
        setMode('idle');
        setSuggested([]);
        onDone?.(result.data);
        return;
      }

      setError(refusalText(result.error));
      setSuggested(suggestedTimes(result.error.details));
    });
  }

  if (!open) {
    const inClinic = appointment.status === 'checked_in' || appointment.status === 'in_consultation';

    // A check-in just done here ends in this state; the number it gave is still worth reading.
    return (
      <div className="space-y-3">
        {notice ? (
          <p role="status" className="text-sm font-medium text-emerald-700">{notice}</p>
        ) : (
          !(inClinic && permissions.canConsult) && <p className="text-sm text-ink-500">{appointmentDetail.nothingToDo}</p>
        )}
        {inClinic && permissions.canConsult && (
          <OpenCaseButton appointmentId={appointment.id} resume={appointment.status === 'in_consultation'} />
        )}
      </div>
    );
  }

  if (mode === 'cancel') {
    return (
      <div className="space-y-2">
        {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
        <CancelForm
          pending={pending}
          onBack={() => setMode('idle')}
          onCancel={(reason) => run(() => cancelAppointmentAction({ appointmentId: appointment.id, reason }))}
        />
      </div>
    );
  }

  if (mode === 'reschedule') {
    return (
      <div className="space-y-2">
        {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
        <RescheduleForm
          appointment={appointment}
          vets={vets}
          pending={pending}
          suggested={suggested}
          onBack={() => setMode('idle')}
          onSubmit={(day, time, staffId) =>
            run(() =>
              rescheduleAppointmentAction({
                appointmentId: appointment.id,
                version: appointment.version,
                startsAt: clinicInstant(day, time),
                staffId,
              }),
            )
          }
        />
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
      {notice && <p role="status" className="text-sm text-emerald-700">{notice}</p>}

      <div className="flex flex-wrap gap-2">
        {permissions.canCheckIn && isToday && (
          <Button
            size="sm"
            disabled={pending}
            onClick={() =>
              run(
                () => checkInAppointmentAction({ appointmentId: appointment.id, idempotencyKey: checkInKey }),
                (data) => {
                  setNotice(appointmentDetail.checkedIn(data.number));
                  setCheckInKey(crypto.randomUUID());
                },
              )
            }
          >
            {text.checkIn}
          </Button>
        )}
        {permissions.canWrite && appointment.status === 'booked' && (
          <Button variant="secondary" size="sm" disabled={pending} onClick={() => run(() => confirmAppointmentAction({ appointmentId: appointment.id }))}>
            {text.confirm}
          </Button>
        )}
        {permissions.canWrite && (
          <Button variant="secondary" size="sm" disabled={pending} onClick={() => setMode('reschedule')}>
            {text.reschedule}
          </Button>
        )}
        {permissions.canWrite && started && (
          <Button variant="secondary" size="sm" disabled={pending} onClick={() => run(() => markNoShowAction({ appointmentId: appointment.id }))}>
            {text.noShow}
          </Button>
        )}
        {permissions.canWrite && (
          <Button variant="danger" size="sm" disabled={pending} onClick={() => setMode('cancel')}>
            {text.cancel}
          </Button>
        )}
      </div>

      {pending && <p className="text-sm text-ink-500">{text.working}</p>}
    </div>
  );
}
