import { doNot, onTheWay } from '../content/emergency-care';
import { emergencyPage } from '../content/emergency-page';
import { CareStepsCard } from './care-steps-card';

/** What to do on the way, beside what not to do. */
export function CareStepsSection() {
  const text = emergencyPage.care;

  return (
    <div className="grid gap-5 lg:grid-cols-2">
      <CareStepsCard heading={text.doHeading} steps={onTheWay} tone="do" />
      <CareStepsCard heading={text.dontHeading} steps={doNot} tone="dont" />
    </div>
  );
}
