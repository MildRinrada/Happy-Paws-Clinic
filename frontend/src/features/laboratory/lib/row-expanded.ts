import { createContext, useContext } from 'react';

/** Whether the worklist row around a component is open. Anything outside a row counts as open. */
export const RowExpandedContext = createContext(true);

export const useRowExpanded = () => useContext(RowExpandedContext);
