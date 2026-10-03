'use client';

import { useState } from 'react';
import { EmptyState } from '@/components/ui/empty-state';
import { appointmentCalendar } from '../../content/appointment-calendar';
import type { CalendarView } from '../../types/calendar.types';
import { AppointmentPanel, type PanelPermissions } from '../detail/appointment-panel';
import { AgendaList } from './agenda-list';
import { CalendarGrid } from './calendar-grid';

interface CalendarBoardProps {
  view: CalendarView;
  permissions: PanelPermissions;
  /** Today in clinic time, so the panel knows whether check-in applies. */
  today: string;
}

/**
 * The day, as a grid on a wide screen and a list on a narrow one, and the panel for whichever
 * appointment was chosen. The panel reads the appointment from the latest server render by id, so
 * after an action it shows the new state without keeping a copy of its own.
 */
export function CalendarBoard({ view, permissions, today }: CalendarBoardProps) {
  const [openId, setOpenId] = useState<string | null>(null);
  const open = view.items.find((item) => item.appointment.id === openId) ?? null;

  if (view.columns.length === 0) {
    return <EmptyState title={appointmentCalendar.empty}>{appointmentCalendar.noVets}</EmptyState>;
  }

  return (
    <>
      <div className="hidden lg:block">
        <CalendarGrid view={view} onOpen={setOpenId} />
      </div>

      <div className="lg:hidden">
        {view.items.length === 0 ? (
          <EmptyState title={appointmentCalendar.empty} />
        ) : (
          <AgendaList items={view.items} onOpen={setOpenId} />
        )}
      </div>

      <AppointmentPanel
        item={open}
        vets={view.vets}
        permissions={permissions}
        isToday={view.day === today}
        onClose={() => setOpenId(null)}
      />
    </>
  );
}
