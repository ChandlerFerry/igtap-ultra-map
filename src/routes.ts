import type { P } from "./geometry";
import { dist, project, type Ring } from "./shifts";

export type Route = {
  points: P[];
  floors: number[];
  rise: number;
  off: number;
  right: boolean;
};

const STEP = 16;
const LISTED = 40;

function crossings(c: P, f: number[], r: Ring): P[] {
  const [x0, y0, x1, y1] = f,
    dx = x1 - x0,
    dy = y1 - y0;
  if (r.use2D)
    return [c[0] - r.t, c[0] + r.t]
      .filter((x) => x >= x0 && x <= x1)
      .map((x) => [x, dx > 0 ? y0 + (dy * (x - x0)) / dx : y0] as P);
  const fx = x0 - c[0],
    fy = y0 - c[1],
    a = dx * dx + dy * dy,
    b = 2 * (fx * dx + fy * dy),
    k = fx * fx + fy * fy - r.t * r.t;
  const disc = b * b - 4 * a * k;
  if (a === 0 || disc < 0) return [];
  const s = Math.sqrt(disc);
  return [(-b - s) / (2 * a), (-b + s) / (2 * a)]
    .filter((u, i, all) => u >= 0 && u <= 1 && (i === 0 || u !== all[0]))
    .map((u) => [x0 + dx * u, y0 + dy * u] as P);
}

function finish(c: P, target: P, radius: number, r: Ring): P | null {
  const candidates: P[] = [project(c, target, r)];
  if (!r.use2D) candidates.push([c[0] - r.t, c[1]], [c[0] + r.t, c[1]]);
  let best: P | null = null,
    near = Infinity;
  for (const p of candidates) {
    const d = Math.hypot(p[0] - target[0], p[1] - target[1]);
    if (p[1] >= c[1] - 1e-6 && d <= radius && d < near) {
      best = p;
      near = d;
    }
  }
  return best;
}

export type Shift = { at: P; floor: number; parent: Shift | null; depth: number };

export function shiftTree(floors: number[][], usable: boolean[], center: P, r: Ring, most = 20): Shift[] {
  const seen = new Set<number>();
  const all: Shift[] = [];
  let frontier: Shift[] = [{ at: center, floor: -1, parent: null, depth: 0 }];
  while (frontier.length) {
    all.push(...frontier);
    const next: Shift[] = [];
    for (const n of frontier) {
      if (n.depth + 2 > most) continue;
      floors.forEach((f, i) => {
        if (!usable[i]) return;
        const [x0, y0, x1, y1] = f,
          near = Math.hypot(
            Math.max(Math.min(x0, x1) - n.at[0], 0, n.at[0] - Math.max(x0, x1)),
            r.use2D ? 0 : Math.max(Math.min(y0, y1) - n.at[1], 0, n.at[1] - Math.max(y0, y1)),
          );
        if (near > r.t || Math.max(dist([x0, y0], n.at, r), dist([x1, y1], n.at, r)) < r.t) return;
        for (const p of crossings(n.at, f, r)) {
          const key = i * 1e5 + Math.round(p[0] / STEP);
          if (seen.has(key)) continue;
          seen.add(key);
          next.push({ at: p, floor: i, parent: n, depth: n.depth + 1 });
        }
      });
    }
    frontier = next;
  }
  return all;
}

export function routesTo(tree: Shift[], r: Ring, target: P, radius: number): Route[] {
  const found: Route[] = [];
  for (const n of tree) {
    const end = finish(n.at, target, radius, r);
    if (!end) continue;
    const points: P[] = [end],
      on: number[] = [];
    for (let m: Shift | null = n; m && m.parent; m = m.parent) {
      points.unshift(m.at);
      on.unshift(m.floor);
    }
    found.push({
      points,
      floors: on,
      rise: end[1] - n.at[1],
      off: Math.hypot(end[0] - target[0], end[1] - target[1]),
      right: end[0] >= n.at[0],
    });
  }
  found.sort((a, b) => a.points.length - b.points.length || b.rise - a.rise || a.off - b.off);
  const kept = new Set<string>();
  return found
    .filter((route) => {
      const key = route.floors.join(",");
      if (kept.has(key)) return false;
      kept.add(key);
      return true;
    })
    .slice(0, LISTED);
}
