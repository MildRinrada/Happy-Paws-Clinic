import { portalAccount } from '../../content/portal-account';

export type FormStatus = { kind: 'idle' } | { kind: 'saved'; message?: string } | { kind: 'error'; message: string };

interface FormFooterProps {
  pending: boolean;
  status: FormStatus;
  label?: string;
}

/** A form's save button, and what happened the last time it was pressed. */
export function FormFooter({ pending, status, label = portalAccount.save }: FormFooterProps) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <button
        type="submit"
        disabled={pending}
        className="h-11 rounded-xl bg-site-teal px-5 text-sm font-semibold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50"
      >
        {pending ? portalAccount.saving : label}
      </button>

      {status.kind === 'saved' && (
        <p role="status" className="text-sm font-medium text-emerald-700">
          {status.message ?? portalAccount.saved}
        </p>
      )}
      {status.kind === 'error' && (
        <p role="alert" className="text-sm text-red-700">
          {status.message}
        </p>
      )}
    </div>
  );
}
