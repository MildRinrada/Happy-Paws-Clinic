'use client';

import { useState } from 'react';
import { ForgotPasswordForm } from './forgot-password-form';

/**
 * Holds the forgot-password form, and starts it over when the visitor wants another link. A new
 * `key` gives a new form with empty state, which is simpler than teaching the action to go back.
 */
export function ForgotPasswordPanel() {
  const [round, setRound] = useState(0);

  return <ForgotPasswordForm key={round} onAgain={() => setRound((value) => value + 1)} />;
}
