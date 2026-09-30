import { dotPath, fy } from "./geometry";
import { ringPath, topArcPath, type Ring } from "./shifts";
import { RING_COLOR, SHIFT_COLOR, WHITE } from "./theme";
import type { Shifts } from "./useShifts";

export function RunShifts({ route, ring }: { route: number[][]; ring: Ring }) {
  return (
    <g pointerEvents="none">
      {route.map((p, i) => (
        <g key={i}>
          <path
            d={ringPath([p[3], p[4]], ring)}
            fill="none"
            stroke={SHIFT_COLOR}
            strokeOpacity={0.35}
            strokeWidth={1}
            strokeDasharray="4 8"
          />
          <path
            d={dotPath(p[0], p[1])}
            stroke={SHIFT_COLOR}
            strokeOpacity={0.6}
            strokeWidth={9}
            strokeLinecap="round"
          />
        </g>
      ))}
    </g>
  );
}

export function ShiftRings({ s, ring, px }: { s: Shifts; ring: Ring; px: number }) {
  const { chain, ordered, anchor, target, leaves } = s;
  const isLast = (i: number) => i === chain.length - 1;
  return (
    <g pointerEvents="none">
      <path
        d={ringPath(s.center, ring)}
        fill="none"
        stroke={SHIFT_COLOR}
        strokeOpacity={0.7}
        strokeWidth={1.5}
        strokeDasharray="10 8"
      />
      {chain.map((p, i) => (
        <path
          key={i}
          d={ringPath(p, ring)}
          fill="none"
          stroke={RING_COLOR}
          strokeOpacity={isLast(i) ? 0.9 : 0.3}
          strokeWidth={isLast(i) ? 2 : 1}
          strokeDasharray="10 8"
        />
      ))}
      {anchor && <path d={ringPath(anchor, ring)} fill="none" stroke={WHITE} strokeOpacity={0.5} strokeWidth={1} />}
      {s.mode === "target" && target && (
        <circle
          cx={target[0]}
          cy={fy(target[1])}
          r={s.radius}
          fill={WHITE}
          fillOpacity={0.12}
          stroke={WHITE}
          strokeWidth={1.5}
        />
      )}
      {leaves && !ring.use2D && (
        <path d={topArcPath(leaves, ring)} fill="none" stroke={SHIFT_COLOR} strokeOpacity={0.8} strokeWidth={2.5} />
      )}
      {ordered.length > 1 && (
        <path
          d={ordered.map((p, i) => `${i ? "L" : "M"}${p[0]} ${fy(p[1])}`).join("")}
          fill="none"
          stroke={RING_COLOR}
          strokeOpacity={0.4}
          strokeWidth={1}
        />
      )}
      {s.closes.map((p, i) => (
        <path key={i} d={dotPath(p[0], p[1])} stroke={SHIFT_COLOR} strokeWidth={10} strokeLinecap="round" />
      ))}
      {chain.map((p, i) => (
        <g key={i}>
          <path
            d={dotPath(p[0], p[1])}
            stroke={isLast(i) ? WHITE : RING_COLOR}
            strokeWidth={12}
            strokeLinecap="round"
          />
          <text
            x={p[0]}
            y={fy(p[1]) - 16 * px}
            fill={RING_COLOR}
            fontSize={14 * px}
            textAnchor="middle"
            className="halo"
            strokeWidth={3 * px}
          >
            {s.label(i)}
          </text>
        </g>
      ))}
    </g>
  );
}
