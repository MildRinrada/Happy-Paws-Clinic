/**
 * Where the token-run waiting-room screen lives (docs/contract-extensions.md § 30). Plain values
 * with no server imports, so client components can use them too. Everything sits under
 * `DISPLAY_PATH`, which is also the path of the screen's cookie: the browser sends the token to
 * these three addresses and nowhere else.
 */
export const DISPLAY_PATH = '/display';

export const DISPLAY_SETUP_PATH = '/display/setup';

export const DISPLAY_STREAM_PATH = '/display/stream';
