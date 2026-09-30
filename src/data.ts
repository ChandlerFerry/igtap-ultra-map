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

export function inState(when: string | null | undefined, pick: StatePick) {
  if (!when) return true;
  const [id, options] = when.split(":");
  return pick[id] === undefined || options.split("|").includes(pick[id]);
}
