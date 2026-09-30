import { capturedStates, type StatePick, type WorldState } from "./data";
import type { P } from "./geometry";

export type View = { x: number; y: number; w: number };
export type Size = { w: number; h: number };

export const viewHeight = (v: View, size: Size) => (v.w * size.h) / size.w;

export function fitView(bounds: number[], size: Size): View {
  const [x0, y0, x1, y1] = bounds,
    aspect = size.h / size.w;
  const w = Math.max(x1 - x0, (y1 - y0) / aspect);
  return { x: (x0 + x1) / 2 - w / 2, y: -(y0 + y1) / 2 - (w * aspect) / 2, w };
}

export const centeredView = (c: P, w: number, size: Size): View => ({
  x: c[0] - w / 2,
  y: -c[1] - (w * size.h) / size.w / 2,
  w,
});

export function zoomAt(v: View, cx: number, cy: number, f: number, width: number): View {
  const mx = v.x + (cx * v.w) / width,
    my = v.y + (cy * v.w) / width;
  return { x: mx - (mx - v.x) * f, y: my - (my - v.y) * f, w: v.w * f };
}

const B62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
const XY = 2 ** 18;
const SPAN = 1024;

export function viewHash(v: View, size: Size, states: WorldState[] | undefined, world: StatePick) {
  const h = viewHeight(v, size),
    clamp = (n: number, top: number) => Math.min(top - 1, Math.max(0, Math.round(n)));
  let n =
    (clamp(v.x + v.w / 2 + XY / 2, XY) * XY + clamp(-(v.y + h / 2) + XY / 2, XY)) * SPAN +
    clamp(40 * Math.log2(Math.min(v.w, h)), SPAN);
  for (const s of states ?? []) n = n * s.options.length + Math.max(0, s.options.indexOf(world[s.id] ?? s.current));
  let out = "";
  do {
    out = B62[n % 62] + out;
    n = Math.floor(n / 62);
  } while (n);
  return out;
}

export function readHash(
  hash: string,
  states: WorldState[] | undefined,
  size: Size,
): { view: View; world: StatePick } | null {
  const code = decodeURIComponent(hash.slice(1)),
    world = capturedStates(states);
  let c: P, span: number;
  if (code.includes(",")) {
    const [x, y, s, ...options] = code.split(",");
    const [cx, cy, sn] = [x, y, s].map(Number);
    if (![cx, cy, sn].every(Number.isFinite) || sn <= 0) return null;
    states?.forEach((st, i) => {
      const o = st.options.find((o) => o === options[i] || o[0] === options[i]);
      if (o) world[st.id] = o;
    });
    c = [cx, cy];
    span = sn;
  } else {
    if (!/^[0-9A-Za-z]{1,9}$/.test(code)) return null;
    let n = 0;
    for (const ch of code) n = n * 62 + B62.indexOf(ch);
    for (const st of [...(states ?? [])].reverse()) {
      world[st.id] = st.options[n % st.options.length];
      n = Math.floor(n / st.options.length);
    }
    span = 2 ** ((n % SPAN) / 40);
    n = Math.floor(n / SPAN);
    c = [Math.floor(n / XY) - XY / 2, (n % XY) - XY / 2];
    if (c[0] >= XY / 2) return null;
  }
  return { view: centeredView(c, span * Math.max(1, size.w / size.h), size), world };
}
