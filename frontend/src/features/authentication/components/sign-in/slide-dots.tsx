import { signInShowcase } from '../../content/sign-in-slides';

interface SlideDotsProps {
  count: number;
  active: number;
  onSelect: (index: number) => void;
}

/** One dot per slide; the active one is drawn as a longer bar. Each is a button to that slide. */
export function SlideDots({ count, active, onSelect }: SlideDotsProps) {
  return (
    <div className="mt-8 flex items-center gap-2">
      {Array.from({ length: count }, (_, index) => (
        <button
          key={index}
          type="button"
          aria-label={signInShowcase.slideLabel(index + 1)}
          aria-current={index === active}
          onClick={() => onSelect(index)}
          className={`h-1.5 rounded-full transition-all ${index === active ? 'w-7 bg-white' : 'w-1.5 bg-white/50 hover:bg-white/80'}`}
        />
      ))}
    </div>
  );
}
