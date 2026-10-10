'use client';

import Link from 'next/link';
import { useEffect, useRef } from 'react';
import { petPaths } from '@/features/pets/lib/pet-paths';
import { Button } from '@/components/ui/button';
import { appointmentCalendar, appointmentDetail } from '../../content/appointment-calendar';
import type { CalendarItem, VetOption } from '../../types/calendar.types';
import { AppointmentStatusBadge } from '../appointment-status-badge';
import { RiskReasons } from '../calendar/risk-reasons';
import { AppointmentActions } from './appointment-actions';
import { DetailRows } from './detail-rows';

export interface PanelPermissions {
  /** `appointment:write`: confirm, move, cancel, no-show. */
  canWrite: boolean;
  /** `appointment:checkin`: put the pet in today's queue. */
  canCheckIn: boolean;
  /** `consult:write`: open the case for a pet that has arrived. */
  canConsult: boolean;
}

interface AppointmentPanelProps {
  item: CalendarItem | null;
  vets: VetOption[];
  permissions: PanelPermissions;
  isToday: boolean;
  onClose: () => void;
}

/** The chosen appointment in a dialog: its details and what can be done with it. */
export function AppointmentPanel({ item, vets, permissions, isToday, onClose }: AppointmentPanelProps) {
  const dialog = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const element = dialog.current;
    if (!element) return;

    if (item && !element.open) element.showModal();
    if (!item && element.open) element.close();
  }, [item]);

  return (
    <dialog
      ref={dialog}
      onClose={onClose}
      aria-labelledby="appointment-panel-title"
      // m-auto: Tailwind's reset zeroes the margin a modal <dialog> centres itself with.
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl border border-ink-200 p-0 backdrop:bg-ink-900/40"
    >
      {item && (
        <div className="space-y-5 p-6">
          <header className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <h2 id="appointment-panel-title" className="font-display text-lg font-semibold text-ink-900">
                <Link href={petPaths.detail(item.appointment.pet_id)} className="hover:text-brand-700 hover:underline">
                  {item.petName ?? appointmentCalendar.unnamedPet}
                </Link>
              </h2>
              {item.petDetail && <p className="text-sm text-ink-600">{item.petDetail}</p>}
            </div>
            <AppointmentStatusBadge status={item.appointment.status} />
          </header>

          {item.allergies.length > 0 && (
            <p role="note" className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-800">
              {appointmentCalendar.allergyList(item.allergies)}
            </p>
          )}

          <DetailRows item={item} />

          <RiskReasons risk={item.risk} />

          {item.appointment.late_cancellation && (
            <p className="text-sm text-amber-800">{appointmentDetail.lateCancellation}</p>
          )}

          <div className="border-t border-ink-100 pt-4">
            <AppointmentActions
              key={item.appointment.id}
              appointment={item.appointment}
              vets={vets}
              permissions={permissions}
              isToday={isToday}
            />
          </div>

          <div className="flex justify-end">
            <Button variant="ghost" size="sm" onClick={() => dialog.current?.close()}>
              {appointmentDetail.close}
            </Button>
          </div>
        </div>
      )}
    </dialog>
  );
}
