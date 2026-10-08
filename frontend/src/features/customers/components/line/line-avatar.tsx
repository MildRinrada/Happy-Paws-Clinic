/* eslint-disable @next/next/no-img-element -- the picture is on LINE's servers and changes when the person changes it; next/image would need LINE's host configured. */

/** A LINE picture, or the first letter of the name on the clinic's green when there is none. */
export function LineAvatar({ name, pictureUrl }: { name?: string; pictureUrl?: string }) {
  if (pictureUrl) {
    return <img src={pictureUrl} alt="" referrerPolicy="no-referrer" className="size-9 shrink-0 rounded-full object-cover" />;
  }

  return (
    <span aria-hidden="true" className="flex size-9 shrink-0 items-center justify-center rounded-full bg-brand-100 text-sm font-bold text-site-teal">
      {name?.trim().charAt(0) || 'L'}
    </span>
  );
}
