import { useEffect, useMemo, useState } from "react";
import type { UltraData } from "./data";
import type { P } from "./geometry";
import { routesTo, shiftTree } from "./routes";
import { dist, meet, onRing, project, type Ring } from "./shifts";

export type Mode = "forward" | "backward" | "target";
export type Shifts = ReturnType<typeof useShifts>;

const CLOSED = 0.5;

export function useShifts(data: UltraData | null, floorOn: boolean[], initialRadius: number) {
  const [mode, setMode] = useState<Mode>("forward");
  const [fwd, setFwd] = useState<P[]>([]);
  const [back, setBack] = useState<P[]>([]);
  const [target, setTarget] = useState<P | null>(null);
  const [radius, setRadiusState] = useState(initialRadius);
  const [pick, setPick] = useState(0);
  const [routesOpen, setRoutesOpen] = useState(false);
  const [placing, setPlacing] = useState(false);

  const ring = useMemo<Ring | null>(() => data && { t: data.rebase.threshold, use2D: data.rebase.use2D }, [data]);
  const center = useMemo<P>(() => [data?.rebase.center[0] ?? 0, data?.rebase.center[1] ?? 0], [data]);
  const runShifts = useMemo<P[]>(() => data?.rebase.route.map((p) => [p[0], p[1]] as P) ?? [], [data]);

  useEffect(() => {
    if (!data || !ring) return;
    const [x0, y0, x1, y1] = data.bounds;
    setFwd(runShifts.length ? runShifts : [project(center, [(x0 + x1) / 2, (y0 + y1) / 2], ring)]);
    setBack([]);
  }, [data, ring, runShifts, center]);

  const targeted = target !== null;
  const tree = useMemo(
    () =>
      data && ring && targeted
        ? shiftTree(
            data.floors,
            data.floors.map((f, i) => floorOn[i] && data.kinds[f[4]] !== "moving"),
            center,
            ring,
          )
        : null,
    [data, ring, targeted, floorOn, center],
  );
  const routes = useMemo(
    () => (tree && ring && target ? routesTo(tree, ring, target, radius) : []),
    [tree, ring, target, radius],
  );
  const route = mode === "target" ? (routes[pick] ?? null) : null;

  const chain = mode === "forward" ? fwd : mode === "backward" ? back : (route?.points ?? []);
  const setChain = mode === "forward" ? setFwd : setBack;
  const last = chain.at(-1) ?? null;
  const prev = chain.length > 1 ? chain[chain.length - 2] : null;
  const anchor = mode === "forward" ? (prev ?? center) : mode === "backward" ? prev : null;
  const leaves = route ? (prev ?? center) : null;
  const gap = mode === "backward" && last && ring ? dist(last, center, ring) - ring.t : null;
  const closed = mode !== "backward" || (gap !== null && Math.abs(gap) <= CLOSED);
  const closes = mode === "backward" && last && ring && !closed ? meet(last, center, ring) : [];
  const ordered = mode === "backward" ? [...chain].reverse() : chain;

  const label = (i: number) =>
    mode !== "backward" ? String(i + 1) : closed ? String(chain.length - i) : i === 0 ? "pick" : `−${i}`;

  const place = (p: P) => {
    if (!ring) return;
    if (mode === "target") {
      setTarget(p);
      setPick(0);
      setRoutesOpen(true);
    } else if (!anchor) setBack([p]);
    else setChain((c) => [...c.slice(0, -1), project(anchor, p, ring)]);
  };

  const extend = () => {
    if (!ring || !last) return;
    const from = mode === "forward" ? anchor : null;
    setChain((c) => [...c, project(last, from ? [2 * last[0] - from[0], 2 * last[1] - from[1]] : center, ring)]);
  };

  const setAngle = (deg: number) => {
    if (anchor && ring) setChain((c) => [...c.slice(0, -1), onRing(anchor, deg, ring)]);
  };

  const setRadius = (r: number) => {
    setRadiusState(r);
    setPick(0);
  };

  const adopt = (points: P[]) => {
    setFwd(points);
    setMode("forward");
  };

  return {
    ring,
    center,
    runShifts,
    mode,
    setMode,
    fwd,
    setFwd,
    back,
    setBack,
    target,
    radius,
    setRadius,
    pick,
    setPick,
    routesOpen,
    setRoutesOpen,
    placing,
    setPlacing,
    routes,
    route,
    chain,
    last,
    anchor,
    leaves,
    gap,
    closed,
    closes,
    ordered,
    label,
    place,
    extend,
    setAngle,
    adopt,
  };
}
