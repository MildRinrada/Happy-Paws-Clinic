interface UploadNotesProps {
  status?: string;
  warning?: string;
  problem?: string;
}

/** What happened to the last pick: done, done but worth a second look, or refused. */
export function UploadNotes({ status, warning, problem }: UploadNotesProps) {
  return (
    <>
      {status && (
        <p role="status" className="text-sm text-emerald-700">
          {status}
        </p>
      )}
      {warning && <p className="text-sm text-amber-700">{warning}</p>}
      {problem && (
        <p role="alert" className="text-sm text-red-700">
          {problem}
        </p>
      )}
    </>
  );
}
