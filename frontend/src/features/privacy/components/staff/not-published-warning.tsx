import { notPublishedWarning } from '../../content/staff-privacy-policy';

/** The prominent warning on the settings page while no version is published. */
export function NotPublishedWarning({ canPublish }: { canPublish: boolean }) {
  return (
    <div role="alert" className="rounded-xl border border-red-200 bg-red-50 px-4 py-3.5 text-red-900">
      <p className="font-semibold">{notPublishedWarning.title}</p>
      <p className="mt-1 text-sm leading-relaxed">{notPublishedWarning.body}</p>
      {!canPublish && <p className="mt-1 text-sm font-medium">{notPublishedWarning.ownerOnly}</p>}
    </div>
  );
}
