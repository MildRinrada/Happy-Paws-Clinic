import type { ComponentType } from 'react';
import type { ServiceIconName } from '../../types/home.types';
import { GroomingIcon } from './icons/grooming-icon';
import { LabIcon } from './icons/lab-icon';
import { SurgeryIcon } from './icons/surgery-icon';

const icons: Record<ServiceIconName, ComponentType<{ className?: string }>> = {
  surgery: SurgeryIcon,
  lab: LabIcon,
  grooming: GroomingIcon,
};

/** Turns the icon name written in the content file into its drawing. */
export function ServiceIcon({ name, className }: { name: ServiceIconName; className?: string }) {
  const Icon = icons[name];

  return <Icon className={className} />;
}
