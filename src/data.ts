export type WorldState = { id: string; label: string; options: string[]; current: string };
export type StatePick = Record<string, string>;

export type Solid = {
  k: "poly" | "line" | "circle" | "capsule" | "area";
  spike: boolean;
  p: number[];
  r: number;
  tone?: "blue" | "orange" | null;
  spring?: number[] | null;
  when?: string | null;
  rings?: number[] | null;
};

export type BoxCategory =
  "secret" | "bonusUnique" | "bonusBoost" | "achievement" | "movement" | "tree" | "unlock" | "course";

export type BoxInfo = {
  upgrade: string;
  gives: string;
  cost: number;
  currency: string;
  cap: number;
  category: BoxCategory;
  fresh: boolean;
  secret: boolean;
  tree: boolean;
  number?: string | null;
  ach?: string | null;
};

export type Marker = {
  kind: string;
  name: string;
  x: number;
  y: number;
  area?: number[][];
  spawn?: number[];
  when?: string | null;
  box?: BoxInfo;
  /** Refill orbs only: JiggleDropScript.ActivationSequence (0-2). An orb is usable when unlockJiggleDrops + moreRefreshOrbs > seq. */
  seq?: number | null;
};

export type Zip = {
  name: string;
  zip: boolean;
  box: number[];
  path: number[][];
  corridor: number[][];
  when?: string | null;
};

export type TreeStep = { tree: number; step: number; via: string | null; box: string | null; x: number; y: number };

export type Label = { name: string; x: number; y: number; when?: string | null };

export type UltraData = {
  bounds: number[];
  kinds: string[];
  floors: number[][];
  floorWhen?: (string | null)[];
  solids: Solid[];
  boxes: number[][];
  boxWhen?: (string | null)[];
  labels: Label[];
  ultraFields: string[];
  ultras: number[][];
  pairFields: string[];
  pairs: number[][];
  grown: Solid[];
  treeSteps: TreeStep[];
  markers: Marker[];
  states?: WorldState[];
  zips?: Zip[];
  rebase: { threshold: number; use2D: boolean; center: number[]; route: number[][] };
};

export const capturedStates = (states: WorldState[] | undefined): StatePick =>
  Object.fromEntries((states ?? []).map((s) => [s.id, s.current]));

/**
 * The ActivationSequence tier a box's upgrade unlocks on the refill orbs, or null if it affects none.
 * An orb is usable when `unlockJiggleDrops + moreRefreshOrbs > ActivationSequence` (JiggleDropScript.SetActiveVisualState),
 * so in the canonical buy order the jiggle-drop unlock turns on the sequence-0 orbs, the more-refresh-orbs box the
 * sequence-1 orbs, and Area 1's overgrowth (a world state, not a box) the sequence-2 ones.
 */
export function orbTier(box: BoxInfo | undefined | null): number | null {
  if (!box) return null;
  if (box.upgrade === "GLOBAL:unlockJiggleDrops") return 0;
  if (box.upgrade === "GLOBAL:moreRefreshOrbs") return 1;
  return null;
}

export function inState(when: string | null | undefined, pick: StatePick) {
  if (!when) return true;
  const [id, options] = when.split(":");
  return pick[id] === undefined || options.split("|").includes(pick[id]);
}
