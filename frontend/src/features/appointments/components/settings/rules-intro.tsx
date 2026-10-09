/** How a rule set acts, as a short list above its form. */
export function RulesIntro({ how }: { how: string[] }) {
  return (
    <ul className="list-disc space-y-1 pl-5 text-sm text-ink-700">
      {how.map((line) => (
        <li key={line}>{line}</li>
      ))}
    </ul>
  );
}
