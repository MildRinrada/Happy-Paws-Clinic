/** What the visit is and for which pet ("ตรวจสุขภาพประจำปี น้องโมจิ"), with the vet under it. */
export function VisitLine({ text, vet }: { text: string; vet?: string }) {
  return (
    <div className="min-w-0">
      <p className="text-sm leading-[22px] text-brand-100">{text}</p>
      {vet && <p className="text-xs leading-[18px] text-brand-100/75">{vet}</p>}
    </div>
  );
}
