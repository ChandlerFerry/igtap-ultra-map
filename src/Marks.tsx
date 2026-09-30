import { memo } from "react";
import type { MarkIcon } from "./Art";
import type { BoxInfo, Label, Marker, TreeStep } from "./data";
import { circlePath, fy, polyPath, starPath } from "./geometry";
import { BG, BOX_KINDS, GROWN_COLOR, LABEL_COLOR, MARKERS, type Look } from "./theme";

const LABEL_PX = 3;
const GATES = new Set(["start", "end", "exit", "falseEnding", "trueEnding"]);

export const layerOf = (m: Marker) => (m.kind === "box" ? `box:${m.box?.category}` : m.kind);
const lookOf = (m: Marker): Look | undefined =>
  m.kind === "box" ? m.box && BOX_KINDS[m.box.category] : MARKERS[m.kind];

function formatCost(cost: number) {
  if (cost < 1e6) return String(cost);
  return cost
    .toExponential(2)
    .replace(/(\.\d*?)0+e/, "$1e")
    .replace(/\.e/, "e")
    .replace("e+", "e");
}

export const boxCost = (b: BoxInfo) => (b.cost <= 0 ? "free" : `${formatCost(b.cost)} ${b.currency}`);
export const boxUses = (b: BoxInfo) => (b.cap === 1 ? "one-off" : `repeatable ×${b.cap}`);
export const secretNumber = (b: BoxInfo) => (b.number ? ` #${b.number}` : "");
export const freshText = (b: BoxInfo) => (b.fresh ? "in the fresh world" : "only later");

function boxTitle(m: Marker) {
  const b = m.box!;
  return (
    `${BOX_KINDS[b.category].text}: ${m.name} · gives ${b.gives} · ${b.upgrade} · ${boxCost(b)} · ${boxUses(b)}` +
    `${b.secret ? ` · secret${secretNumber(b)}` : ""}${b.ach ? ` · ${b.ach}` : ""} · ${freshText(b)}${b.tree ? " · tree upgrade" : ""}`
  );
}

export const treeStepText = (t: TreeStep) =>
  t.step
    ? `Tree ${t.tree} · purchase ${t.step} grows this${t.via ? ` (bought at ${t.via})` : ""}${t.box ? `; its box: ${t.box}` : ""}`
    : `Tree ${t.tree} · no box found that grows this`;

function shapePath(shape: Look["shape"], x: number, y: number, s: number) {
  if (shape === "diamond") return `M${x} ${y - s * 1.3}L${x + s} ${y}L${x} ${y + s * 1.3}L${x - s} ${y}Z`;
  if (shape === "circle") return circlePath(x, y, s);
  if (shape === "star") return starPath(x, y, s);
  return `M${x - s} ${y - s}h${2 * s}v${2 * s}h${-2 * s}Z`;
}

function AreaOutline({ area, color }: { area: number[][]; color: string }) {
  return (
    <path
      d={area.map(polyPath).join("")}
      fill={color}
      fillOpacity={0.1}
      stroke={color}
      strokeWidth={1.5}
      strokeDasharray="5 3"
      pointerEvents="none"
    />
  );
}

function BoxLabel({ m, color, x, y, px }: { m: Marker; color: string; x: number; y: number; px: number }) {
  return (
    <text x={x} y={y} fill={color} fontSize={11 * px} textAnchor="middle" className="halo" strokeWidth={3 * px}>
      {`${m.name} · ${boxCost(m.box!)}`}
    </text>
  );
}

function Mark({ m, look, px, icon }: { m: Marker; look: Look; px: number; icon?: MarkIcon }) {
  const title = <title>{`${look.text}: ${m.name}`}</title>;
  if (m.spawn)
    return (
      <rect
        x={m.spawn[0]}
        y={fy(m.spawn[3])}
        width={m.spawn[2] - m.spawn[0]}
        height={m.spawn[3] - m.spawn[1]}
        fill="none"
        stroke={look.color}
        strokeWidth={2}
        pointerEvents="stroke"
      >
        {title}
      </rect>
    );
  if (icon)
    return (
      <image
        href={icon.href}
        x={m.x + icon.dx - icon.w / 2}
        y={fy(m.y + icon.dy + icon.h / 2)}
        width={icon.w}
        height={icon.h}
      >
        {title}
      </image>
    );
  if (GATES.has(m.kind) && m.area?.length)
    return (
      <path d={m.area.map(polyPath).join("")} fill={look.color} fillOpacity={0.15} stroke={look.color} strokeWidth={2}>
        {title}
      </path>
    );
  const x = m.x,
    y = fy(m.y);
  if (m.kind === "box" && m.area)
    return (
      <g>
        <path
          d={m.area.map(polyPath).join("")}
          fill={look.color}
          fillOpacity={0.12}
          stroke={look.color}
          strokeWidth={2}
        >
          <title>{boxTitle(m)}</title>
        </path>
        {px <= LABEL_PX && (
          <BoxLabel
            m={m}
            color={look.color}
            x={x}
            y={fy(Math.max(...m.area.flatMap((p) => p.filter((_, k) => k % 2 === 1)))) - 5 * px}
            px={px}
          />
        )}
      </g>
    );
  const s = (look.shape === "small" ? 4 : look.shape === "star" ? 10 : 7) * px;
  return (
    <g>
      <path
        d={shapePath(look.shape, x, y, s)}
        fill={look.color}
        fillOpacity={look.shape === "circle" ? 0.8 : 1}
        stroke={BG}
        strokeWidth={1}
      >
        {title}
      </path>
      {m.kind === "box" && px <= LABEL_PX && <BoxLabel m={m} color={look.color} x={x} y={y - s - 5 * px} px={px} />}
    </g>
  );
}

export const Marks = memo(function Marks({
  markers,
  steps,
  px,
  kinds,
  areas,
  checkpointAreas,
  showSteps,
  icons,
}: {
  markers: Marker[];
  steps: TreeStep[];
  px: number;
  kinds: Record<string, boolean>;
  areas: boolean;
  checkpointAreas: boolean;
  showSteps: boolean;
  icons: Record<string, MarkIcon>;
}) {
  return (
    <g>
      {checkpointAreas &&
        markers.map(
          (m, i) =>
            m.kind === "checkpoint" &&
            m.area && <AreaOutline key={`trigger${i}`} area={m.area} color={MARKERS.checkpoint.color} />,
        )}
      {areas &&
        markers.map(
          (m, i) =>
            m.kind !== "checkpoint" &&
            m.kind !== "box" &&
            m.area &&
            kinds[m.kind] && <AreaOutline key={`area${i}`} area={m.area} color={MARKERS[m.kind]?.color} />,
        )}
      {markers.map((m, i) => {
        const look = lookOf(m);
        return look && kinds[layerOf(m)] ? <Mark key={i} m={m} look={look} px={px} icon={icons[m.kind]} /> : null;
      })}
      {showSteps &&
        steps.map((t, i) => (
          <text
            key={i}
            x={t.x}
            y={fy(t.y) + 5 * px}
            fill={GROWN_COLOR}
            fontSize={14 * px}
            fontWeight={700}
            textAnchor="middle"
            className="halo"
            strokeWidth={3 * px}
          >
            {t.step || "?"}
            <title>{treeStepText(t)}</title>
          </text>
        ))}
    </g>
  );
});

export function Labels({ labels, px, show }: { labels: Label[]; px: number; show: (name: string) => boolean }) {
  return labels.map((l) => {
    const name = l.name.split(" · ").filter(show).join(" · ");
    return (
      name && (
        <text
          key={l.name}
          x={l.x}
          y={fy(l.y) - 30 * px}
          fill={LABEL_COLOR}
          fontSize={14 * px}
          textAnchor="middle"
          pointerEvents="none"
        >
          {name}
        </text>
      )
    );
  });
}
