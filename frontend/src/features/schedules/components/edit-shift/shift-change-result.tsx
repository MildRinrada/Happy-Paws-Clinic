import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { schedulePage } from '../../content/schedule-page';
import { shiftEditText } from '../../content/shift-edit';
import type { TimeOffConflict } from '../../types/schedule.types';
import { AppointmentConflicts } from '../appointment-conflicts';

interface ShiftChangeResultProps {
  /** "บันทึกเวลาใหม่แล้ว" or "ลบเวรแล้ว". */
  done: string;
  conflicts: TimeOffConflict[];
  names: Record<string, string>;
  onClose: () => void;
}

/** After a change or removal that left booked appointments outside anyone's shift: what to call about. */
export function ShiftChangeResult({ done, conflicts, names, onClose }: ShiftChangeResultProps) {
  const text = shiftEditText;

  return (
    <div className="space-y-3">
      <Alert tone="success">{done}</Alert>
      <AppointmentConflicts conflicts={conflicts} names={names} heading={text.conflicts} emptyText={text.noConflicts} />
      <div className="flex justify-end">
        <Button variant="secondary" onClick={onClose}>
          {schedulePage.dialogs.close}
        </Button>
      </div>
    </div>
  );
}
