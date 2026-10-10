import { parsePolicyBody } from '../lib/policy-body';
import { PolicyBlock } from './policy-block';

/**
 * A section's body as readers see it. The public page and the console's preview both draw through
 * here, so what staff see before publishing is what customers will see.
 */
export function PolicyBody({ body }: { body: string }) {
  return (
    <>
      {parsePolicyBody(body).map((block, index) => (
        <PolicyBlock key={index} block={block} />
      ))}
    </>
  );
}
