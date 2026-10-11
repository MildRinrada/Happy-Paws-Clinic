'use client';

import type { CopyWeekPlan } from '../../types/schedule.types';
import { AddShiftForm } from '../add-shift-form';
import { CopyWeekForm } from '../copy-week/copy-week-form';
import { EditShiftForm } from '../edit-shift/edit-shift-form';
import { TimeOffForm } from '../time-off-form';
import type { RosterDialogRequest } from './roster-dialogs-context';

/** What every roster dialog needs from the page. */
export interface RosterDialogSetup {
  /** Who can be rostered here, in the roster's order. */
  people: { id: string; name: string }[];
  branchId: string;
  /** The day a form starts on when the click did not name one. */
  defaultDay: string;
  /** What "คัดลอกเวรสัปดาห์ก่อน" would copy. */
  copy: CopyWeekPlan;
}

interface RosterDialogBodyProps {
  request: RosterDialogRequest;
  setup: RosterDialogSetup;
  /** Shuts the dialog. */
  onDone: () => void;
}

/** The form for what was asked: add, leave, change or remove, or copy. */
export function RosterDialogBody({ request, setup, onDone }: RosterDialogBodyProps) {
  const names = Object.fromEntries(setup.people.map((person) => [person.id, person.name]));

  switch (request.kind) {
    case 'addShift':
      return (
        <AddShiftForm
          people={setup.people}
          branchId={setup.branchId}
          defaultDay={request.day ?? setup.defaultDay}
          defaultStaffId={request.staffId}
          onDone={onDone}
        />
      );
    case 'timeOff':
      return <TimeOffForm people={setup.people} defaultDay={setup.defaultDay} />;
    case 'editShift':
      return (
        <EditShiftForm
          chip={request.chip}
          name={names[request.staffId] ?? request.staffId}
          day={request.day}
          names={names}
          onDone={onDone}
        />
      );
    case 'copyWeek':
      return <CopyWeekForm branchId={setup.branchId} plan={setup.copy} names={names} onDone={onDone} />;
  }
}
