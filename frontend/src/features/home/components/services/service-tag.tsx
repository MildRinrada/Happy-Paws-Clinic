import type { ServiceTag as ServiceTagData, ServiceTagTone } from '../../types/home.types';

const tones: Record<ServiceTagTone, string> = {
  neutral: 'bg-[#f3f3f3] text-[#555555]',
  // Something the owner must do beforehand, such as fasting.
  warning: 'bg-[#fde8e5] text-[#e0301e]',
  // Something the clinic does for them, such as a reminder.
  notice: 'bg-[#fdf1dc] text-[#dc9a2b]',
};

/** A small label on a service card: how long it takes, or what to know before coming. */
export function ServiceTag({ tag }: { tag: ServiceTagData }) {
  return (
    <li className={`flex h-[26px] items-center whitespace-nowrap rounded-md px-2 text-[13px] ${tones[tag.tone]}`}>
      {tag.label}
    </li>
  );
}
