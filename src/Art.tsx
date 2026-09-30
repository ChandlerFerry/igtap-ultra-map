import { memo, useEffect, useState } from "react";
import type { StatePick } from "./data";
import { fy } from "./geometry";

export type ArtIndex = {
  tile: number;
  scale: number;
  states: Record<string, Record<string, string>[]>;
  icons?: Record<string, { file: string; w: number; h: number; dx: number; dy: number }>;
};
export type MarkIcon = { href: string; w: number; h: number; dx: number; dy: number };
export type ArtTile = { href: string; x: number; y: number; size: number };

export const ART_BASE = `${import.meta.env.BASE_URL}art/`;

export const artIcons = (art: ArtIndex | null): Record<string, MarkIcon> =>
  Object.fromEntries(
    Object.entries(art?.icons ?? {}).map(([k, v]) => [
      k,
      { href: `${ART_BASE}${v.file}.webp`, w: v.w, h: v.h, dx: v.dx, dy: v.dy },
    ]),
  );

const artKey = (pick: StatePick) =>
  Object.keys(pick)
    .sort()
    .map((k) => `${k}:${pick[k]}`)
    .join(",");

const cache = new Map<string, ArtTile[]>();

export function artTiles(
  art: ArtIndex | null,
  pick: StatePick,
  x0: number,
  y0: number,
  x1: number,
  y1: number,
  px: number,
): ArtTile[] {
  if (!art) return [];
  let key = artKey(pick),
    levels = art.states[key];
  if (!levels) {
    const full = { ...pick };
    for (const k of Object.keys(art.states))
      for (const e of k.split(",")) {
        const [id, option] = e.split(":");
        full[id] ??= option;
      }
    levels = art.states[(key = artKey(full))];
  }
  if (!levels?.length) return [];
  const level = Math.max(0, Math.min(levels.length - 1, Math.floor(Math.log2(art.scale * px))));
  const size = art.tile / (art.scale / 2 ** level);
  const i0 = Math.floor(x0 / size),
    i1 = Math.floor(x1 / size),
    j0 = Math.floor(y0 / size),
    j1 = Math.floor(y1 / size);
  const cacheKey = `${key}|${level}|${i0},${j0},${i1},${j1}`;
  let out = cache.get(cacheKey);
  if (!out) {
    const files = levels[level];
    out = [];
    for (let i = i0; i <= i1; i++)
      for (let j = j0; j <= j1; j++) {
        const f = files[`${i},${j}`];
        if (f) out.push({ href: `${ART_BASE}${f}.webp`, x: i * size, y: j * size, size });
      }
    if (cache.size > 32) cache.clear();
    cache.set(cacheKey, out);
  }
  return out;
}

const loaded = (href: string) =>
  new Promise<void>((done) => {
    const img = new Image();
    img.onload = img.onerror = () => done();
    img.src = href;
  });

const tileKey = (t: ArtTile) => t.href + t.x + "," + t.y;

export const Art = memo(function Art({ tiles }: { tiles: ArtTile[] }) {
  const [shown, setShown] = useState<ArtTile[]>([]);
  useEffect(() => {
    let live = true;
    Promise.all(tiles.map((t) => loaded(t.href))).then(() => {
      if (live) setShown(tiles);
    });
    return () => {
      live = false;
    };
  }, [tiles]);
  const now = new Set(tiles.map(tileKey));
  const all = shown === tiles ? tiles : [...shown.filter((t) => !now.has(tileKey(t))), ...tiles];
  return (
    <g pointerEvents="none">
      {all.map((t) => (
        <image
          key={tileKey(t)}
          href={t.href}
          x={t.x}
          y={fy(t.y + t.size)}
          width={t.size}
          height={t.size}
          preserveAspectRatio="none"
        />
      ))}
    </g>
  );
});
