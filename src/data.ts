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
  /** Buy-max / exempt-from-prestige boxes: the course they target (upgradeBox.NumberOfCourseToAffect). */
  course?: string | null;
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
 * and three things bump that sum: the jiggle-drop unlock, the more-refresh-orbs tree box (350M NP), and the Omni Dash
 * box's first buy (70B NP, upgradeBox.DoBuyUpgrade: it also overgrows Area 1). In buy order that's sequence 0, 1, 2.
 * (The second tree box named "MoreOrbs" actually gives makeCourseBuyMax and touches no orbs.)
 */
export function orbTier(box: BoxInfo | undefined | null): number | null {
  if (!box) return null;
  if (box.upgrade === "GLOBAL:unlockJiggleDrops") return 0;
  if (box.upgrade === "GLOBAL:moreRefreshOrbs") return 1;
  if (box.upgrade === "Movement:OmniDash") return 2;
  return null;
}

export function inState(when: string | null | undefined, pick: StatePick) {
  if (!when) return true;
  const [id, options] = when.split(":");
  return pick[id] === undefined || options.split("|").includes(pick[id]);
}
