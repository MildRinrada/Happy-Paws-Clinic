import { lineKindLabel } from '../content/line-kinds';
import type { InvoiceLineKind } from '../types/billing.types';

const tones: Record<InvoiceLineKind, string> = {
  service: 'bg-brand-50 text-brand-700',
  treatment: 'bg-sky-50 text-sky-800',
  medicine: 'bg-violet-50 text-violet-800',
  other: 'bg-ink-100 text-ink-600',
};

/**
 * What a bill line is for, beside its description. Medicine lines arrive from the dispensary
 * without the cashier having typed them, so the counter needs to tell a drug from a service at a
 * glance.
 */
export function LineKindTag({ kind }: { kind: InvoiceLineKind }) {
  return (
    <span className={`inline-flex shrink-0 rounded px-1.5 py-0.5 text-xs font-medium ${tones[kind]}`}>{lineKindLabel[kind]}</span>
  );
}
