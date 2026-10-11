'use client';

import { useContext, useEffect } from 'react';
import { SaveBarActionsContext } from '../components/save-bar/save-bar-context';

/**
 * For a settings form that keeps its own save button (an editor with more than one action, such as
 * save and publish): not in the save bar, but leaving the page while `dirty` still asks first.
 */
export function useLeaveWarning(formId: string, dirty: boolean) {
  const { register, unregister } = useContext(SaveBarActionsContext);

  useEffect(() => {
    // Never drawn in the bar, so it needs no name there.
    register({ formId, section: '', dirty, pending: false, failed: false, inBar: false }, () => {});
  }, [register, formId, dirty]);

  useEffect(() => () => unregister(formId), [unregister, formId]);
}
