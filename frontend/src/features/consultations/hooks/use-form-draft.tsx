'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import type { ConsultationNotes } from '../types/consultation.types';

/** One drafted line of the examination form. */
export interface DraftedFinding {
  body_system: string;
  is_normal: boolean;
  note?: string;
}

/**
 * Text offered to the case's forms from outside them: today, the AI's SOAP draft
 * (docs/contract-extensions.md § 34). The forms decide what to do with it, and nothing is saved
 * by offering: the notes form saves as it always does once its fields change, and the examination
 * form waits for its own save button.
 */
export interface FormDraft {
  notes: Partial<ConsultationNotes>;
  findings: DraftedFinding[];
  /** Tells one offer from the next, so the same draft offered twice is applied twice. */
  stamp: number;
}

interface FormDraftChannel {
  draft: FormDraft | null;
  offer: (draft: Omit<FormDraft, 'stamp'>) => void;
}

const FormDraftContext = createContext<FormDraftChannel | null>(null);

/** Wraps the case's forms so a panel beside them can hand them a draft. */
export function FormDraftProvider({ children }: { children: ReactNode }) {
  const [draft, setDraft] = useState<FormDraft | null>(null);
  const offer = useCallback((next: Omit<FormDraft, 'stamp'>) => setDraft({ ...next, stamp: Date.now() }), []);
  const channel = useMemo(() => ({ draft, offer }), [draft, offer]);

  return <FormDraftContext.Provider value={channel}>{children}</FormDraftContext.Provider>;
}

/** For the panel that offers a draft. Undefined outside a provider. */
export function useOfferFormDraft() {
  return useContext(FormDraftContext)?.offer;
}

/** For a form: runs `apply` once for each draft offered. Does nothing outside a provider. */
export function useIncomingFormDraft(apply: (draft: FormDraft) => void) {
  const draft = useContext(FormDraftContext)?.draft ?? null;
  const applied = useRef<number | null>(null);
  const latest = useRef(apply);

  useEffect(() => {
    latest.current = apply;
  });

  useEffect(() => {
    if (draft && draft.stamp !== applied.current) {
      applied.current = draft.stamp;
      latest.current(draft);
    }
  }, [draft]);
}
