/**
 * The clinical module's routes: the consulting room, the lab, the pharmacy counter and the
 * treatment templates. Each part registers its own; `records.ts` adds the visits, medicines and
 * lab work to the pet page's timeline and holds the helpers other modules read a pet's file with.
 */
import './consultation-handlers';
import './lab-handlers';
import './rx-handlers';
import './records';
