import Link from 'next/link';
import { petDocuments } from '../../content/pet-documents';

interface DocumentSentProps {
  /** The title the clinic will see, or the file's name. */
  name: string;
  recordsHref?: string;
  onAnother: () => void;
}

/** The form's place once the file is in: what happened, and the two likely next steps. */
export function DocumentSent({ name, recordsHref, onAnother }: DocumentSentProps) {
  const text = petDocuments.sent;

  return (
    <div role="status" className="space-y-4">
      <div className="rounded-xl bg-brand-50 px-4 py-3">
        <p className="text-sm font-semibold text-site-teal">{text.title}</p>
        <p className="mt-1 break-all text-sm text-ink-700">{text.body(name)}</p>
      </div>
      <div className="flex flex-wrap gap-x-6 gap-y-2">
        <button type="button" onClick={onAnother} className="text-sm font-semibold text-site-teal hover:underline">
          {text.another}
        </button>
        {recordsHref && (
          <Link href={recordsHref} className="text-sm font-semibold text-site-teal hover:underline">
            {text.records}
          </Link>
        )}
      </div>
    </div>
  );
}
