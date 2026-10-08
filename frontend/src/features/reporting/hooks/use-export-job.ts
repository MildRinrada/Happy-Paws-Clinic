'use client';

import { useCallback, useEffect, useRef, useState, useTransition } from 'react';
import type { ActionError } from '@/lib/actions/action-result';
import { exportStatusAction, startExportAction } from '../actions/report-export-actions';
import { POLL_ATTEMPTS, POLL_INTERVAL_MS } from '../lib/exportable-reports';
import type { ReportExport, ReportKind } from '../types/report-export.types';

interface ExportRequest {
  report: ReportKind;
  from: string;
  to: string;
  branchId?: string;
}

/**
 * One export job seen from the browser: start it, then ask its status every two seconds until the
 * file is ready or it failed. After a minute the polling stops and a button takes over, because a
 * long export is still being built in the background and nothing is lost by asking later.
 */
export function useExportJob() {
  const [job, setJob] = useState<ReportExport>();
  const [error, setError] = useState<ActionError | 'unreachable'>();
  const [polling, setPolling] = useState(false);
  const [starting, startTransition] = useTransition();
  const attempts = useRef(0);

  const refresh = useCallback(async (id: string) => {
    try {
      const result = await exportStatusAction({ id });
      if (result.ok) setJob(result.data);
      else setError(result.error);
      return result.ok ? result.data : undefined;
    } catch {
      setError('unreachable');
      return undefined;
    }
  }, []);

  useEffect(() => {
    if (!polling || !job || job.status === 'succeeded' || job.status === 'failed') {
      return;
    }

    const timer = setTimeout(async () => {
      attempts.current += 1;
      const next = await refresh(job.id);
      if (!next || attempts.current >= POLL_ATTEMPTS) setPolling(false);
    }, POLL_INTERVAL_MS);

    return () => clearTimeout(timer);
  }, [job, polling, refresh]);

  function start(request: ExportRequest) {
    setError(undefined);
    setJob(undefined);
    attempts.current = 0;

    startTransition(async () => {
      try {
        const result = await startExportAction({ report: request.report, from: request.from, to: request.to, branch_id: request.branchId });
        if (!result.ok) {
          setError(result.error);
          return;
        }
        setJob(result.data);
        setPolling(true);
      } catch {
        setError('unreachable');
      }
    });
  }

  /** Asks once more, and resumes polling if the job is still under way. */
  function check() {
    if (!job) return;
    attempts.current = 0;
    void refresh(job.id).then((next) => setPolling(Boolean(next && (next.status === 'queued' || next.status === 'running'))));
  }

  return { job, error, starting, polling, start, check };
}
