import type { UltraData } from "./data";
import { fy, type P } from "./geometry";
import type { Which } from "./settings";
import { BANDS, bandIndex, SHIFT_COLOR, ULTRA_COLOR, WHITE } from "./theme";

export type Ultra = {
  fromX: number;
  fromY: number;
  side: number;
  x: number;
  y: number;
  drop: number;
  hyper: number;
  hold: number;
  delay: number;
  jump: number;
  gap: number;
  jump2: number;
  landX: number;
  prevX: number;
  shift: number;
  prevY: number;
};

export type Pair = {
  index: number;
  from: number;
  to: number;
  drop: number;
  ax: number;
  ay: number;
  bx: number;
  by: number;
  line: boolean;
  ultra: Ultra | null;
};

export type PairsByFloor = { out: Pair[][]; into: Pair[][] };

export function decodePairs(d: UltraData): Pair[] {
  const ultras = d.ultras.map((u) => Object.fromEntries(d.ultraFields.map((f, i) => [f, u[i]])) as Ultra);
  const [from, to, drop, ax, ay, bx, by, line, ultra] = [
    "from",
    "to",
    "drop",
    "ax",
    "ay",
    "bx",
    "by",
    "line",
    "ultra",
  ].map((n) => d.pairFields.indexOf(n));
  return d.pairs.map((r, index) => ({
    index,
    from: r[from],
    to: r[to],
    drop: r[drop],
    ax: r[ax],
    ay: r[ay],
    bx: r[bx],
    by: r[by],
    line: r[line] === 1,
    ultra: r[ultra] >= 0 ? ultras[r[ultra]] : null,
  }));
}

export const isShiftOnly = (p: Pair) => !p.line && !!p.ultra?.shift;

export const pairColor = (p: Pair) => (isShiftOnly(p) ? SHIFT_COLOR : BANDS[bandIndex(p.drop)][1]);

export function matches(p: Pair, which: Which) {
  if (which === "all") return p.line || !p.ultra?.shift;
  if (which === "engine") return !!p.ultra && !p.ultra.shift;
  return !!p.ultra?.shift;
}

export function groupByFloor(pairs: Pair[], floorCount: number): PairsByFloor {
  const out: Pair[][] = Array.from({ length: floorCount }, () => []),
    into: Pair[][] = Array.from({ length: floorCount }, () => []);
  for (const p of pairs) {
    out[p.from].push(p);
    into[p.to].push(p);
  }
  for (const list of [...out, ...into]) list.sort((a, b) => b.drop - a.drop);
  return { out, into };
}

export function pairCounts({ out, into }: PairsByFloor, i: number) {
  const count = (list: Pair[], dir: string) =>
    `${list.length} ${dir}${list[0] ? ` (↓${list[0].drop.toFixed(0)})` : ""}`;
  return `${count(out[i], "out")} · ${count(into[i], "in")}`;
}

const bow = (p: Pair) => Math.max(24, Math.abs(p.bx - p.ax) * 0.25);

export const arrowPath = (p: Pair) =>
  `M${p.ax} ${fy(p.ay)}Q${(p.ax + p.bx) / 2} ${fy(Math.max(p.ay, p.by) + bow(p))} ${p.bx} ${fy(p.by)}`;

export const dropLabelAt = (p: Pair): P => [(p.ax + p.bx) / 2, Math.max(p.ay, p.by) + bow(p) / 2];

export type Leg = { a: P; b: P; color: string; dash: boolean };

export function ultraLegs(u: Ultra): Leg[] {
  const launch: P = [u.fromX, u.fromY],
    first: P = [u.prevX, u.prevY],
    land: P = [u.landX, u.y];
  const chained = u.hyper === 2;
  return [
    ...(chained ? [{ a: launch, b: first, color: WHITE, dash: true }] : []),
    { a: chained ? first : launch, b: land, color: WHITE, dash: false },
    { a: land, b: [u.x, u.y], color: ULTRA_COLOR, dash: false },
  ];
}
