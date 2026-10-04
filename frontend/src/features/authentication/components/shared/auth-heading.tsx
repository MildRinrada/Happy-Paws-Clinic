/** A page title and the line under it, sized like the sign-in page's "ยินดีต้อนรับกลับมา". */
export function AuthHeading({ heading, subheading }: { heading: string; subheading: string }) {
  return (
    <div>
      <h1 className="font-display text-[30px] font-bold leading-tight text-ink-900">{heading}</h1>
      <p className="mt-2 text-lg text-site-muted">{subheading}</p>
    </div>
  );
}
