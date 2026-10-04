interface FormStatusProps {
  /** A refusal that names no field, or a network failure. */
  message?: string;
  saved?: boolean;
  savedText: string;
}

/** The line beside a form's save button: what went wrong, or that the save went through. */
export function FormStatus({ message, saved, savedText }: FormStatusProps) {
  if (message) {
    return (
      <p role="alert" className="text-sm text-red-700">
        {message}
      </p>
    );
  }

  return saved ? (
    <p role="status" className="text-sm text-emerald-700">
      {savedText}
    </p>
  ) : null;
}
