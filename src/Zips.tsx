import { memo, useState } from "react";
import type { Zip } from "./data";
import { fy, polyPath } from "./geometry";
import { ZIP_COLOR } from "./theme";

export const zipAt = (zips: Zip[], [x, y]: [number, number], reach: number) =>
  zips.find((z) => x >= z.box[0] - reach && x <= z.box[2] + reach && y >= z.box[1] - reach && y <= z.box[3] + reach);

function zipTitle(z: Zip, pinned: boolean) {
  const legs = z.path.length - 1;
  return `${z.zip ? "Zip mover" : "Moving platform"}: ${z.name} · ${legs} leg${legs === 1 ? "" : "s"} · tap to ${pinned ? "unpin" : "pin"} its path`;
}

function arrowPath([ax, ay]: number[], [bx, by]: number[], head: number) {
  const len = Math.hypot(bx - ax, by - ay) || 1,
    ux = (bx - ax) / len,
    uy = (by - ay) / len;
  return (
    `M${ax} ${fy(ay)}L${bx} ${fy(by)}` +
    `M${bx - head * (ux + uy * 0.6)} ${fy(by - head * (uy - ux * 0.6))}L${bx} ${fy(by)}` +
    `L${bx - head * (ux - uy * 0.6)} ${fy(by - head * (uy + ux * 0.6))}`
  );
}

function ZipPath({ zip, head }: { zip: Zip; head: number }) {
  const w = zip.box[2] - zip.box[0],
    h = zip.box[3] - zip.box[1];
  return (
    <g pointerEvents="none">
      {zip.corridor.map((c, k) => (
        <path
          key={k}
          d={polyPath(c)}
          fill={ZIP_COLOR}
          fillOpacity={0.18}
          stroke={ZIP_COLOR}
          strokeOpacity={0.6}
          strokeWidth={1}
          strokeDasharray="4 3"
        />
      ))}
      {zip.path.slice(1).map(([x, y], k) => (
        <rect
          key={k}
          x={x - w / 2}
          y={fy(y + h / 2)}
          width={w}
          height={h}
          fill="none"
          stroke={ZIP_COLOR}
          strokeWidth={1.5}
        />
      ))}
      {zip.path.slice(1).map((b, k) => (
        <path
          key={`a${k}`}
          d={arrowPath(zip.path[k], b, head)}
          fill="none"
          stroke="#fff"
          strokeWidth={2}
          strokeLinecap="round"
        />
      ))}
    </g>
  );
}

export const Zips = memo(function Zips({ zips, px, pinned }: { zips: Zip[]; px: number; pinned: ReadonlySet<Zip> }) {
  const [hot, setHot] = useState<Zip | null>(null);
  const lit = (z: Zip) => z === hot || pinned.has(z);
  return (
    <g>
      {zips.map((z, i) => (
        <rect
          key={i}
          x={z.box[0]}
          y={fy(z.box[3])}
          width={z.box[2] - z.box[0]}
          height={z.box[3] - z.box[1]}
          fill={ZIP_COLOR}
          fillOpacity={lit(z) ? 0.25 : 0.08}
          stroke={ZIP_COLOR}
          strokeWidth={lit(z) ? 2.5 : 1.5}
          style={{ cursor: "pointer" }}
          onPointerEnter={() => setHot(z)}
          onPointerLeave={() => setHot((h) => (h === z ? null : h))}
        >
          <title>{zipTitle(z, pinned.has(z))}</title>
        </rect>
      ))}
      {zips.filter(lit).map((z, i) => (
        <ZipPath key={i} zip={z} head={14 * px} />
      ))}
    </g>
  );
});
