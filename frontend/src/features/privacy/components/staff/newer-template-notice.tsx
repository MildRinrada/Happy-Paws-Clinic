import { newerTemplateNotice } from '../../content/staff-privacy-policy';

/** Says a release shipped a newer template. The clinic's notice is not touched until staff make a draft from it. */
export function NewerTemplateNotice({ version }: { version: string }) {
  return (
    <div role="status" className="rounded-xl border border-sky-200 bg-sky-50 px-4 py-3.5 text-sky-900">
      <p className="font-semibold">{newerTemplateNotice.title}</p>
      <p className="mt-1 break-words text-sm leading-relaxed">{newerTemplateNotice.body(version)}</p>
    </div>
  );
}
