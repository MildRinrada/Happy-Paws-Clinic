import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { petSummaryText } from '../../content/pet-summary';
import { AiDraftBadge } from '../common/ai-draft-badge';
import { PetSummaryPanel } from './pet-summary-panel';

/** The "สรุปด้วย AI" card on a pet's page. The page draws it only when the assistant is on for this session. */
export function PetSummaryCard({ petId }: { petId: string }) {
  return (
    <Card>
      <CardHeader title={petSummaryText.title} aside={<AiDraftBadge />} />
      <CardBody>
        <PetSummaryPanel petId={petId} />
      </CardBody>
    </Card>
  );
}
