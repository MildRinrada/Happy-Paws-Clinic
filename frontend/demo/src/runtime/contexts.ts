import { createContext } from 'react';
import type { Place } from './navigation';

/** Where the page on screen is, for `usePathname()`. */
export const PlaceContext = createContext<Place>({ pathname: '/', search: '', hash: '' });

/** The dynamic segments of the page on screen, for `useParams()`. */
export const ParamsContext = createContext<Record<string, string>>({});
