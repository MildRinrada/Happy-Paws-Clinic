import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { treatmentPlan } from '../../content/treatment-plan';
import { TreatmentPlanForm, type TreatmentPlanFormProps } from './treatment-plan-form';

/**
 * Planning a course of visits for this patient. Shown only to those who may write a consultation
 * (`consult:write`). The contract has no read for plans, so the card holds the form and says
 * where the booked sessions can be found.
 */
export function TreatmentPlanCard(props: TreatmentPlanFormProps) {
  const text = treatmentPlan;

  return (
    <Card>
      <CardHeader title={text.title} />
      <CardBody className="space-y-3">
        <p className="text-sm text-ink-600">{text.intro}</p>
        <p className="text-xs text-ink-500">{text.noList}</p>
        <TreatmentPlanForm {...props} />
      </CardBody>
    </Card>
  );
}
