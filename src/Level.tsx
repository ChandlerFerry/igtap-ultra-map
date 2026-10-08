import { memo } from "react";
import { inState, type Marker, type Solid, type StatePick, type TreeStep } from "./data";
import { circlePath, fy, polyPath } from "./geometry";
import {
  BOX_COLOR,
  GROWN_COLOR,
  LEVEL_COLOR,
  ORB_HI_COLOR,
  SPIKE_COLOR,
  SPRING_COLOR,
  TONE_COLORS,
  treeStepColor,
  WHITE,
} from "./theme";

type Tone = "blue" | "orange";
const TONES: Tone[] = ["blue", "orange"];

type SpringDir = { cx: number; cy: number; ux: number; uy: number; reach: number };

export type LevelPaths = {
  ground: string;
  lines: string;
  spikes: string;
  spikeLines: string;
  boxes: string;
  springs: string;
  springDirs: SpringDir[];
  tone: Record<Tone, { ground: string; spikes: string }>;
  areas: string[];
  spikeAreas: string[];
};

export type LevelShow = {
  spikes: boolean;
  boxes: boolean;
  blue: boolean;
  orange: boolean;
  springs: boolean;
};

function ringPaths(s: Solid) {
  let d = "",
    at = 0;
  for (const n of s.rings ?? [s.p.length]) {
    d += polyPath(s.p.slice(at, at + n));
    at += n;
  }
  return d;
}

function springDir(s: Solid): SpringDir {
  const [ux, uy] = s.spring!,
    n = s.p.length / 2;
  let cx = 0,
    cy = 0,
    reach = 0;
  for (let i = 0; i < n; i++) {
    cx += s.p[2 * i] / n;
    cy += s.p[2 * i + 1] / n;
  }
  for (let i = 0; i < n; i++) reach = Math.max(reach, (s.p[2 * i] - cx) * ux + (s.p[2 * i + 1] - cy) * uy);
  return { cx, cy, ux, uy, reach };
}

function solidPath(s: Solid) {
  if (s.k === "circle") return circlePath(s.p[0], fy(s.p[1]), s.r);
  let d = `M${s.p[0]} ${fy(s.p[1])}`;
  for (let i = 2; i < s.p.length; i += 2) d += `L${s.p[i]} ${fy(s.p[i + 1])}`;
  return s.k === "poly" ? d + "Z" : d;
}

export function levelPaths(solids: Solid[], boxes: number[][], pick: StatePick): LevelPaths {
  const out: LevelPaths = {
    ground: "",
    lines: "",
    spikes: "",
    spikeLines: "",
    boxes: boxes.map(polyPath).join(""),
    springs: "",
    springDirs: [],
    tone: { blue: { ground: "", spikes: "" }, orange: { ground: "", spikes: "" } },
    areas: [],
    spikeAreas: [],
  };
  for (const s of solids) {
    if (!inState(s.when, pick)) continue;
    if (s.spring) {
      out.springs += polyPath(s.p);
      out.springDirs.push(springDir(s));
    } else if (s.k === "area") {
      (s.spike ? out.spikeAreas : out.areas).push(ringPaths(s));
    } else {
      const d = solidPath(s),
        closed = s.k === "poly" || s.k === "circle";
      if (s.tone) out.tone[s.tone][s.spike ? "spikes" : "ground"] += d;
      else if (s.spike) out[closed ? "spikes" : "spikeLines"] += d;
      else out[closed ? "ground" : "lines"] += d;
    }
  }
  return out;
}

const ARROW_PX = 6;

function springArrows(dirs: SpringDir[]) {
  const head = 16;
  return dirs
    .map(({ cx, cy, ux, uy, reach }) => {
      const len = reach + 40,
        tx = cx + ux * len,
        ty = cy + uy * len;
      return `M${cx} ${fy(cy)}L${tx} ${fy(ty)}M${tx - head * (ux + uy)} ${fy(ty - head * (uy - ux))}L${tx} ${fy(ty)}L${tx - head * (ux - uy)} ${fy(ty - head * (uy + ux))}`;
    })
    .join("");
}

export const Level = memo(function Level({ paths, show, px }: { paths: LevelPaths; show: LevelShow; px: number }) {
  return (
    <g pointerEvents="none">
      <path d={paths.ground} fill={LEVEL_COLOR} fillOpacity={0.55} />
      {paths.areas.map((d, i) => (
        <path key={i} d={d} fill={LEVEL_COLOR} fillOpacity={0.55} fillRule="evenodd" />
      ))}
      <path d={paths.lines} fill="none" stroke={LEVEL_COLOR} strokeWidth={1.5} />
      {show.spikes && (
        <>
          <path d={paths.spikes} fill={SPIKE_COLOR} fillOpacity={0.85} />
          {paths.spikeAreas.map((d, i) => (
            <path key={i} d={d} fill={SPIKE_COLOR} fillOpacity={0.85} fillRule="evenodd" />
          ))}
          <path d={paths.spikeLines} fill="none" stroke={SPIKE_COLOR} strokeWidth={2} />
        </>
      )}
      {TONES.map(
        (t) =>
          show[t] && (
            <g key={t}>
              <path
                d={paths.tone[t].ground}
                fill={TONE_COLORS[t]}
                fillOpacity={0.45}
                stroke={TONE_COLORS[t]}
                strokeWidth={1}
              />
              <path
                d={paths.tone[t].spikes}
                fill="none"
                stroke={TONE_COLORS[t]}
                strokeWidth={2.5}
                strokeDasharray="3 2"
              />
            </g>
          ),
      )}
      {show.boxes && <path d={paths.boxes} fill={BOX_COLOR} fillOpacity={0.12} stroke={BOX_COLOR} strokeWidth={1} />}
      {show.springs && (
        <path
          d={paths.springs}
          fill={SPRING_COLOR}
          fillOpacity={0.8}
          stroke={SPRING_COLOR}
          strokeWidth={1.5}
          strokeLinejoin="round"
        />
      )}
      <path
        d={show.springs && px <= ARROW_PX ? springArrows(paths.springDirs) : ""}
        fill="none"
        stroke={SPRING_COLOR}
        strokeWidth={2}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </g>
  );
});

export const Hazards = memo(function Hazards({ paths, show }: { paths: LevelPaths; show: LevelShow }) {
  return (
    <g pointerEvents="none">
      <path d={paths.spikes} fill={SPIKE_COLOR} fillOpacity={0.3} stroke={SPIKE_COLOR} strokeWidth={1.5} />
      {paths.spikeAreas.map((d, i) => (
        <path
          key={i}
          d={d}
          fill={SPIKE_COLOR}
          fillOpacity={0.3}
          fillRule="evenodd"
          stroke={SPIKE_COLOR}
          strokeWidth={1.5}
        />
      ))}
      <path d={paths.spikeLines} fill="none" stroke={SPIKE_COLOR} strokeWidth={2} />
      {TONES.map(
        (t) =>
          show[t] && (
            <path
              key={t}
              d={paths.tone[t].spikes}
              fill={SPIKE_COLOR}
              fillOpacity={0.2}
              stroke={TONE_COLORS[t]}
              strokeWidth={2}
              strokeDasharray="3 2"
            />
          ),
      )}
    </g>
  );
});

/** One path per tree segment (index into treeSteps), from the grown shapes that make it up. */
export const segmentPaths = (grown: Solid[], grownStep: number[], steps: TreeStep[]) =>
  grown.reduce(
    (d, s, i) => {
      if (d[grownStep[i]] !== undefined) d[grownStep[i]] += polyPath(s.p);
      return d;
    },
    steps.map(() => ""),
  );

export type SegmentLabel = { step: number; x: number; y: number };

// A segment's ground can sit in far-apart pieces (tree 1 segment 17 owns a long-fall collider ~6000 u away), so label
// each cluster of its shapes, not the center of them all.
const CLUSTER_GAP = 1000;

/** Where to number each segment: the center of every cluster of its shapes. */
export function segmentLabels(grown: Solid[], grownStep: number[]): SegmentLabel[] {
  const clusters: { step: number; b: number[] }[] = [];
  grown.forEach((s, i) => {
    const xs = s.p.filter((_, k) => k % 2 === 0),
      ys = s.p.filter((_, k) => k % 2 === 1),
      b = [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)];
    // ponytail: single-pass merge, a shape bridging two earlier clusters won't join them; fine for 1-3 shapes a segment.
    const near = clusters.find(
      (c) =>
        c.step === grownStep[i] && Math.max(c.b[0] - b[2], b[0] - c.b[2], c.b[1] - b[3], b[1] - c.b[3]) < CLUSTER_GAP,
    );
    if (near)
      near.b = [
        Math.min(near.b[0], b[0]),
        Math.min(near.b[1], b[1]),
        Math.max(near.b[2], b[2]),
        Math.max(near.b[3], b[3]),
      ];
    else clusters.push({ step: grownStep[i], b });
  });
  return clusters.map(({ step, b }) => ({ step, x: (b[0] + b[2]) / 2, y: (b[1] + b[3]) / 2 }));
}

/** Grown by the same box (box names repeat, so compare where it is). */
const sameGrower = (a: TreeStep, b: TreeStep | null) =>
  !!a.viaAt && !!b?.viaAt && a.viaAt[0] === b.viaAt[0] && a.viaAt[1] === b.viaAt[1];

/**
 * Tree segments numbered by their in-game index and filled by how many tree buys deep they are; hovering/tapping one
 * lights up every segment its box grows and rings that box.
 */
export const TreeSegments = memo(function TreeSegments({
  paths,
  labels,
  steps,
  hi,
  grower,
  px,
}: {
  paths: string[];
  labels: SegmentLabel[];
  steps: TreeStep[];
  hi: TreeStep | null;
  grower: Marker | null;
  px: number;
}) {
  const last: Record<number, number> = {};
  for (const t of steps) last[t.tree] = Math.max(last[t.tree] ?? 1, t.step);
  const colorOf = (t: TreeStep) => (t.step ? treeStepColor((t.step - 1) / Math.max(1, last[t.tree] - 1)) : GROWN_COLOR);
  return (
    <g pointerEvents="none">
      {steps.map((t, i) => {
        const on = sameGrower(t, hi);
        return (
          <path
            key={i}
            d={paths[i]}
            fill={colorOf(t)}
            fillOpacity={on ? 0.75 : hi ? 0.2 : 0.45}
            stroke={on ? WHITE : colorOf(t)}
            strokeWidth={on ? 3 : 1.5}
            strokeLinejoin="round"
          />
        );
      })}
      {labels.map((l, i) => (
        <text
          key={i}
          x={l.x}
          y={fy(l.y) + 5 * px}
          fill={sameGrower(steps[l.step], hi) ? WHITE : colorOf(steps[l.step])}
          fontSize={14 * px}
          fontWeight={700}
          textAnchor="middle"
          className="halo"
          strokeWidth={3 * px}
        >
          {steps[l.step].seg}
        </text>
      ))}
      {grower && (
        <circle cx={grower.x} cy={fy(grower.y)} r={16 * px} fill="none" stroke={ORB_HI_COLOR} strokeWidth={2} />
      )}
    </g>
  );
});
