import { fy, type P } from "./geometry";

export type Ring = { t: number; use2D: boolean };

export const dist = (a: P, b: P, r: Ring) => (r.use2D ? Math.abs(a[0] - b[0]) : Math.hypot(a[0] - b[0], a[1] - b[1]));

export const ringPath = (c: P, r: Ring) =>
  r.use2D
    ? `M${c[0] - r.t} -1e6V1e6M${c[0] + r.t} -1e6V1e6`
    : `M${c[0] - r.t} ${fy(c[1])}a${r.t} ${r.t} 0 1 0 ${2 * r.t} 0a${r.t} ${r.t} 0 1 0 ${-2 * r.t} 0Zh${2 * r.t}`;

export const topArcPath = (c: P, r: Ring) => `M${c[0] - r.t} ${fy(c[1])}a${r.t} ${r.t} 0 0 1 ${2 * r.t} 0`;

export function project(c: P, p: P, r: Ring): P {
  if (r.use2D) return [c[0] + (p[0] >= c[0] ? r.t : -r.t), p[1]];
  const dx = p[0] - c[0],
    dy = p[1] - c[1],
    len = Math.hypot(dx, dy);
  return len < 1e-9 ? [c[0] + r.t, c[1]] : [c[0] + (dx * r.t) / len, c[1] + (dy * r.t) / len];
}

export const angleOf = (c: P, p: P) => ((Math.atan2(p[1] - c[1], p[0] - c[0]) * 180) / Math.PI + 360) % 360;

export const onRing = (c: P, deg: number, r: Ring): P => [
  c[0] + r.t * Math.cos((deg * Math.PI) / 180),
  c[1] + r.t * Math.sin((deg * Math.PI) / 180),
];

export function meet(a: P, b: P, r: Ring): P[] {
  if (r.use2D)
    return [a[0] - r.t, a[0] + r.t]
      .filter((x) => Math.abs(Math.abs(x - b[0]) - r.t) < 1e-3)
      .map((x) => [x, (a[1] + b[1]) / 2]);
  const dx = b[0] - a[0],
    dy = b[1] - a[1],
    d = Math.hypot(dx, dy);
  if (d === 0 || d > 2 * r.t) return [];
  const h = Math.sqrt(r.t * r.t - (d * d) / 4),
    mx = a[0] + dx / 2,
    my = a[1] + dy / 2;
  return [
    [mx - (dy / d) * h, my + (dx / d) * h],
    [mx + (dy / d) * h, my - (dx / d) * h],
  ];
}
