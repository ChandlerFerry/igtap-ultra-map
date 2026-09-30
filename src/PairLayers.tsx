import { floorPath, fy, segmentPath } from "./geometry";
import { arrowPath, dropLabelAt, pairColor, type Leg, type Pair } from "./pairs";
import { BANDS, IN_COLOR, SHIFT_COLOR, ULTRA_COLOR, WHITE } from "./theme";

const arrowId = (color: string) => `ultra-arrow-${color.slice(1)}`;
const arrowEnd = (color: string) => `url(#${arrowId(color)})`;
const ARROW_COLORS = [...BANDS.map(([, c]) => c), SHIFT_COLOR, IN_COLOR, WHITE, ULTRA_COLOR];

export function ArrowDefs({ px }: { px: number }) {
  return (
    <defs>
      {ARROW_COLORS.map((c) => (
        <marker
          key={c}
          id={arrowId(c)}
          viewBox="0 0 10 10"
          refX={9}
          refY={5}
          markerUnits="userSpaceOnUse"
          markerWidth={12 * px}
          markerHeight={12 * px}
          orient="auto"
        >
          <path d="M0 0L10 5L0 10Z" fill={c} />
        </marker>
      ))}
    </defs>
  );
}

function FloorHighlight({ floor, color }: { floor: number[]; color: string }) {
  return <path d={floorPath(floor)} stroke={color} strokeWidth={6} strokeLinecap="round" />;
}

export function FloorPairs({
  floor,
  floors,
  out,
  into,
  focus,
}: {
  floor: number[];
  floors: number[][];
  out: Pair[];
  into: Pair[];
  focus: Pair | null;
}) {
  const faded = (p: Pair) => focus && focus !== p;
  const width = (p: Pair) => (focus === p ? 3 : 1.25);
  return (
    <g pointerEvents="none">
      {out.map((p) => (
        <FloorHighlight key={`t${p.index}`} floor={floors[p.to]} color={pairColor(p)} />
      ))}
      {into.map((p) => (
        <FloorHighlight key={`s${p.index}`} floor={floors[p.from]} color={IN_COLOR} />
      ))}
      {out.map((p) => (
        <path
          key={`o${p.index}`}
          d={arrowPath(p)}
          fill="none"
          stroke={pairColor(p)}
          strokeOpacity={faded(p) ? 0.25 : 0.8}
          strokeWidth={width(p)}
          strokeDasharray={p.line ? undefined : "5 4"}
          markerEnd={arrowEnd(pairColor(p))}
        />
      ))}
      {into.map((p) => (
        <path
          key={`i${p.index}`}
          d={arrowPath(p)}
          fill="none"
          stroke={IN_COLOR}
          strokeOpacity={faded(p) ? 0.2 : 0.7}
          strokeWidth={width(p)}
          strokeDasharray={p.line ? "2 3" : "5 4"}
          markerEnd={arrowEnd(IN_COLOR)}
        />
      ))}
      <path d={floorPath(floor)} stroke={WHITE} strokeWidth={8} strokeLinecap="round" />
    </g>
  );
}

export function UltraLegs({ legs }: { legs: Leg[] }) {
  return (
    <g pointerEvents="none">
      {legs.map((l, i) => (
        <path
          key={i}
          d={segmentPath(l.a[0], l.a[1], l.b[0], l.b[1])}
          stroke={l.color}
          strokeWidth={i === legs.length - 1 ? 3 : 2}
          strokeDasharray={l.dash ? "6 4" : undefined}
          markerEnd={arrowEnd(l.color)}
        />
      ))}
    </g>
  );
}

export function DropLabel({ pair, px }: { pair: Pair; px: number }) {
  const [x, y] = dropLabelAt(pair);
  return (
    <text
      x={x}
      y={fy(y) - 8 * px}
      fill={WHITE}
      fontSize={13 * px}
      textAnchor="middle"
      className="halo"
      strokeWidth={3 * px}
      pointerEvents="none"
    >
      ↓ {pair.drop.toFixed(1)} u
    </text>
  );
}
