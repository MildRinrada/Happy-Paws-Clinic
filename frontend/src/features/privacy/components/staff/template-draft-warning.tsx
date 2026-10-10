import { templateDraftWarning } from '../../content/staff-privacy-policy';

/** On a draft made from the shipped template: it is the developer's starting point, not legal advice. */
export function TemplateDraftWarning() {
  return (
    <aside role="note" className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3.5 text-amber-900">
      <p className="font-semibold">{templateDraftWarning.title}</p>
      <p className="mt-1 text-sm leading-relaxed">{templateDraftWarning.body}</p>
    </aside>
  );
}
