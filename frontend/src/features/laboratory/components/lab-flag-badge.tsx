import { Badge, type BadgeTone } from '@/components/ui/badge';
import { labPage } from '../content/lab-page';
import type { LabFlag } from '../types/lab.types';

const tones: Record<LabFlag, BadgeTone> = { normal: 'neutral', low: 'red', high: 'red', abnormal: 'red' };

/** "ธงค่าผิดปกติ". A value with no range and no judgement has no flag, and shows none. */
export function LabFlagBadge({ flag }: { flag?: LabFlag }) {
  if (!flag) return null;
  return <Badge tone={tones[flag]}>{labPage.results.flags[flag]}</Badge>;
}
