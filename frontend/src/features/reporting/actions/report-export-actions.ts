'use server';

import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { getReportExport } from '../api/getReportExport';
import { startReportExport } from '../api/startReportExport';
import type { ReportExport } from '../types/report-export.types';

/** Starting an export and asking how it is going. The backend checks the scopes and the branch again. */

const day = z.string().regex(/^\d{4}-\d{2}-\d{2}$/);

const startInput = z.object({
  report: z.enum(['revenue', 'services', 'retention', 'staff_performance', 'appointments', 'queue', 'workload']),
  format: z.enum(['csv', 'xlsx', 'pdf']),
  from: day,
  to: day,
  branch_id: z.string().max(40).optional(),
});

export async function startExportAction(values: z.infer<typeof startInput>): Promise<ActionResult<ReportExport>> {
  const input = startInput.parse(values);

  return runAction(() => startReportExport(input));
}

const statusInput = z.object({ id: z.string().min(1).max(40) });

export async function exportStatusAction(values: z.infer<typeof statusInput>): Promise<ActionResult<ReportExport>> {
  const { id } = statusInput.parse(values);

  return runAction(() => getReportExport(id));
}
