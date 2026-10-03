import { Badge } from '@/components/ui/badge';
import { appointmentStatusLabel } from '../lib/status-labels';
import type { AppointmentStatus } from '../types/appointment.types';

export function AppointmentStatusBadge({ status }: { status: AppointmentStatus }) {
  const label = appointmentStatusLabel[status];

  return <Badge tone={label.tone}>{label.text}</Badge>;
}
