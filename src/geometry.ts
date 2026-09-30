export type P = [number, number];

export const fy = (y: number) => -y;
export const fmt = (p: P) => `${p[0].toFixed(0)}, ${p[1].toFixed(0)}`;

export const dotPath = (x: number, y: number) => `M${x} ${fy(y)}h0`;
export const segmentPath = (ax: number, ay: number, bx: number, by: number) => `M${ax} ${fy(ay)}L${bx} ${fy(by)}`;
export const polyPath = (p: number[]) =>
  p.reduce((d, v, i) => (i % 2 ? `${d} ${fy(v)}` : `${d}${i ? "L" : "M"}${v}`), "") + "Z";
export const circlePath = (x: number, y: number, r: number) =>
  `M${x - r} ${y}a${r} ${r} 0 1 0 ${2 * r} 0a${r} ${r} 0 1 0 ${-2 * r} 0Z`;

export function starPath(x: number, y: number, r: number) {
  let d = "";
  for (let i = 0; i < 10; i++) {
    const a = -Math.PI / 2 + (i * Math.PI) / 5,
      k = i % 2 ? r * 0.45 : r;
    d += `${i ? "L" : "M"}${x + k * Math.cos(a)} ${y + k * Math.sin(a)}`;
  }
  return d + "Z";
}

export const floorPath = (f: number[]) => segmentPath(f[0], f[1], f[2], f[3]);
export const floorY = (f: number[], x: number) =>
  f[2] > f[0] ? f[1] + (f[3] - f[1]) * Math.min(1, Math.max(0, (x - f[0]) / (f[2] - f[0]))) : f[1];
export const where = (f: number[]) =>
  `${f[0].toFixed(0)}…${f[2].toFixed(0)} @ ${f[1] === f[3] ? f[1].toFixed(0) : `${f[1].toFixed(0)}→${f[3].toFixed(0)}`}`;
