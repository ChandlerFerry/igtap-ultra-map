import type { BoxCategory } from "./data";

export const BG = "#07111f";
export const WHITE = "#ffffff";
export const LABEL_COLOR = "#e8eef8";

export const KIND_COLORS: Record<string, string> = { metal: "#4fc3f7", moss: "#81c784", moving: "#ffb74d" };
export const KIND_TEXT: Record<string, string> = { metal: "Ground", moss: "Moss", moving: "Moving platform" };
export const kindColor = (kind: string) => KIND_COLORS[kind] ?? "#fff";

export const BANDS: [number, string][] = [
  [16, "#fff176"],
  [64, "#ffb74d"],
  [160, "#ff7043"],
  [400, "#e040fb"],
  [Infinity, "#7c4dff"],
];
export const bandIndex = (drop: number) => BANDS.findIndex(([top]) => drop < top);

export const SHIFT_COLOR = "#76ff03";
export const RING_COLOR = "#18ffff";
export const IN_COLOR = "#80deea";
export const ULTRA_COLOR = "#ffd54f";
export const ZIP_COLOR = "#ffb74d";
export const TONE_COLORS: Record<"blue" | "orange", string> = { blue: "#42a5f5", orange: "#ffa726" };
export const GROWN_COLOR = "#c5a26b";
export const SPRING_COLOR = "#ffd54f";
export const SPIKE_COLOR = "#e5484d";
export const LEVEL_COLOR = "#8193ab";
export const BOX_COLOR = "#bcaaa4";

export type MarkerShape = "square" | "small" | "diamond" | "circle" | "star";
export type Look = { color: string; text: string; shape: MarkerShape };

export const MARKERS: Record<string, Look> = {
  start: { color: "#3ddc97", text: "Start gate", shape: "square" },
  end: { color: "#ff6b6b", text: "End gate", shape: "square" },
  exit: { color: "#90a4ae", text: "Preventative Gate", shape: "square" },
  falseEnding: { color: "#f48fb1", text: "False ending", shape: "square" },
  trueEnding: { color: "#ffe082", text: "True ending", shape: "square" },
  checkpoint: { color: "#4fc3f7", text: "Checkpoint", shape: "small" },
  teleporter: { color: "#b388ff", text: "Teleporter", shape: "diamond" },
  dashRefill: { color: "#81c784", text: "Dash refill", shape: "circle" },
  jumpRefill: { color: "#b39ddb", text: "Jump refill", shape: "circle" },
  fullRefill: { color: "#fff59d", text: "Full refill", shape: "circle" },
};

export const BOX_KINDS: Record<BoxCategory, Look> = {
  secret: { color: "#ffd740", text: "Secret", shape: "star" },
  bonusUnique: { color: "#66bb6a", text: "Unlock bonus", shape: "diamond" },
  bonusBoost: { color: "#b0bec5", text: "Boost bonus", shape: "diamond" },
  achievement: { color: "#ce93d8", text: "Achievement box", shape: "star" },
  movement: { color: "#29b6f6", text: "Movement/ability upgrade", shape: "diamond" },
  tree: { color: "#c5a26b", text: "Tree upgrade", shape: "square" },
  unlock: { color: "#ff7043", text: "One-off unlock", shape: "square" },
  course: { color: "#90caf9", text: "Course upgrade", shape: "small" },
};
