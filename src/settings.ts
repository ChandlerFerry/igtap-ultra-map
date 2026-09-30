export type Which = "all" | "engine" | "shift";
export type Direction = "both" | "out" | "in";
export type Settings = {
  layers: Record<string, boolean>;
  which: Which;
  direction: Direction;
  radius: number;
  legend: boolean;
};

export const DEFAULT_LAYERS: Record<string, boolean> = {
  "floor:metal": false,
  "floor:moss": false,
  "floor:moving": false,
  drops: false,
  art: true,
  background: false,
  zips: true,
  shapes: false,
  spikes: true,
  blue: true,
  orange: true,
  springs: true,
  boxes: false,
  grown: false,
  "marker:checkpoint": true,
  checkpointAreas: false,
  areas: false,
  "marker:start": false,
  "marker:end": false,
  "marker:exit": false,
  "marker:falseEnding": true,
  "marker:trueEnding": true,
  "marker:teleporter": false,
  "marker:dashRefill": true,
  "marker:jumpRefill": true,
  "marker:fullRefill": true,
  "marker:box:secret": true,
  "marker:box:bonusUnique": true,
  "marker:box:bonusBoost": true,
  "marker:box:achievement": true,
  "marker:box:movement": true,
  "marker:box:unlock": true,
  "marker:box:tree": false,
  "marker:box:course": false,
  rings: false,
  run: false,
  labels: true,
  bonusLabels: false,
};

const KEY = "igtap-ultra-map:settings";

export function loadSettings(): Settings {
  const defaults: Settings = {
    layers: { ...DEFAULT_LAYERS },
    which: "all",
    direction: "both",
    radius: 256,
    legend: typeof matchMedia === "undefined" || matchMedia("(min-width: 801px)").matches,
  };
  try {
    const s = JSON.parse(localStorage.getItem(KEY) ?? "null") as Partial<Settings> | null;
    if (!s || typeof s !== "object") return defaults;
    const layers = { ...defaults.layers };
    if (s.layers && typeof s.layers === "object")
      for (const [k, v] of Object.entries(s.layers)) if (typeof v === "boolean") layers[k] = v;
    return {
      layers,
      which: s.which === "engine" || s.which === "shift" ? s.which : "all",
      direction: s.direction === "out" || s.direction === "in" ? s.direction : "both",
      radius: typeof s.radius === "number" && s.radius >= 1 ? s.radius : defaults.radius,
      legend: typeof s.legend === "boolean" ? s.legend : defaults.legend,
    };
  } catch {
    return defaults;
  }
}

export function saveSettings(s: Settings) {
  try {
    localStorage.setItem(KEY, JSON.stringify(s));
  } catch {}
}
