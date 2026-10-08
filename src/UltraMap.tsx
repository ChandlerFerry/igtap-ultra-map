import { useEffect, useMemo, useState } from "react";
import { Art, artIcons, artTiles, type ArtIndex } from "./Art";
import { BoxCard, FloorCard, Hint, type Listed } from "./Cards";
import {
  capturedStates,
  inState,
  orbTier,
  type Marker,
  type StatePick,
  type TreeStep,
  type UltraData,
  type Zip,
} from "./data";
import { floorPath, floorY, fmt, insidePoly, where, type P } from "./geometry";
import { Legend } from "./Legend";
import { Hazards, Level, levelPaths, segmentLabels, segmentPaths, TreeSegments, type LevelShow } from "./Level";
import { Labels, layerOf, Marks } from "./Marks";
import { ArrowDefs, DropLabel, FloorPairs, UltraLegs } from "./PairLayers";
import { decodePairs, groupByFloor, isShiftOnly, matches, pairCounts, ultraLegs, type Pair } from "./pairs";
import { RouteModal } from "./RouteModal";
import { loadSettings, saveSettings, type Direction, type Which } from "./settings";
import { ShiftBar } from "./ShiftBar";
import { RunShifts, ShiftRings } from "./ShiftLayers";
import { dist } from "./shifts";
import { StatePicker } from "./StatePicker";
import { BANDS, BG, bandIndex, BOX_KINDS, kindColor, MARKERS, SHIFT_COLOR, WHITE } from "./theme";
import { usePanZoom } from "./usePanZoom";
import { useShareLink } from "./useShareLink";
import { useShifts } from "./useShifts";
import { centeredView, fitView, viewHeight } from "./view";
import { zipAt, Zips } from "./Zips";

const DATA_URL = `${import.meta.env.BASE_URL}ultras-VMAN.json`;
const ART_URL = `${import.meta.env.BASE_URL}art/index.json`;

export function UltraMap() {
  const [data, setData] = useState<UltraData | null>(null);
  const [art, setArt] = useState<ArtIndex | null>(null);
  const [failure, setFailure] = useState("");
  const [saved] = useState(loadSettings);
  const [layers, setLayers] = useState(saved.layers);
  const [legendOpen, setLegendOpen] = useState(saved.legend);
  const [which, setWhich] = useState<Which>(saved.which);
  const [direction, setDirection] = useState<Direction>(saved.direction);
  const [world, setWorld] = useState<StatePick>({});
  const [treeTip, setTreeTip] = useState<TreeStep | null>(null);
  const [hoverTree, setHoverTree] = useState<TreeStep | null>(null);
  const [boxTip, setBoxTip] = useState<Marker | null>(null);
  const [hoverMark, setHoverMark] = useState<Marker | null>(null);
  const [cursor, setCursor] = useState<P | null>(null);
  const [hoverFloor, setHoverFloor] = useState(-1);
  const [floor, setFloor] = useState(-1);
  const [focus, setFocus] = useState<Pair | null>(null);
  const [pinned, setPinned] = useState<ReadonlySet<Zip>>(new Set());

  useEffect(() => {
    fetch(DATA_URL)
      .then((r) => {
        if (!r.ok) throw new Error(`ultras-VMAN.json: HTTP ${r.status}`);
        return r.json() as Promise<UltraData>;
      })
      .then(
        (d) => {
          setData(d);
          setWorld(capturedStates(d.states));
        },
        (e) => setFailure(e instanceof Error ? e.message : String(e)),
      );
    fetch(ART_URL)
      .then((r) => (r.ok ? (r.json() as Promise<ArtIndex>) : null))
      .then(setArt, () => setArt(null));
  }, []);

  const floorOn = useMemo(
    () => (data?.floors ?? []).map((_, i) => inState(data?.floorWhen?.[i], world)),
    [data, world],
  );
  const shifts = useShifts(data, floorOn, saved.radius);
  const { ring } = shifts;

  useEffect(
    () => saveSettings({ layers, which, direction, radius: shifts.radius, legend: legendOpen }),
    [layers, which, direction, shifts.radius, legendOpen],
  );

  const pairs = useMemo(() => (data ? decodePairs(data) : []), [data]);
  const byFloor = useMemo(
    () =>
      groupByFloor(
        pairs.filter((p) => floorOn[p.from] && floorOn[p.to] && matches(p, which)),
        data?.floors.length ?? 0,
      ),
    [pairs, floorOn, which, data],
  );
  const dropPaths = useMemo(() => {
    const paths = [...BANDS.map(() => ""), ""];
    (data?.floors ?? []).forEach((f, i) => {
      const best = byFloor.into[i][0];
      if (best) paths[isShiftOnly(best) ? BANDS.length : bandIndex(best.drop)] += floorPath(f);
    });
    return paths.map((d, i) => ({ d, color: i < BANDS.length ? BANDS[i][1] : SHIFT_COLOR }));
  }, [data, byFloor]);
  const floorPaths = useMemo(
    () =>
      (data?.kinds ?? []).map((_, k) =>
        (data?.floors ?? [])
          .filter((f, i) => floorOn[i] && f[4] === k)
          .map(floorPath)
          .join(""),
      ),
    [data, floorOn],
  );
  const level = useMemo(
    () =>
      levelPaths(
        data?.solids ?? [],
        (data?.boxes ?? []).filter((_, i) => inState(data?.boxWhen?.[i], world)),
        world,
      ),
    [data, world],
  );
  const markers = useMemo(() => (data?.markers ?? []).filter((m) => inState(m.when, world)), [data, world]);
  const treeSteps = useMemo(() => data?.treeSteps ?? [], [data]);
  const treePaths = useMemo(
    () => (data?.grownStep ? segmentPaths(data.grown, data.grownStep, treeSteps) : []),
    [data, treeSteps],
  );
  const treeLabels = useMemo(() => (data?.grownStep ? segmentLabels(data.grown, data.grownStep) : []), [data]);
  // The box marker that grows each segment: the box nearest its grow box's position (markers sit at the trigger's center).
  const growers = useMemo(() => {
    const boxes = (data?.markers ?? []).filter((m) => m.kind === "box");
    return treeSteps.map((t) => {
      const at = t.viaAt;
      if (!at) return null;
      return boxes.reduce<Marker | null>(
        (best, m) =>
          !best || Math.hypot(m.x - at[0], m.y - at[1]) < Math.hypot(best.x - at[0], best.y - at[1]) ? m : best,
        null,
      );
    });
  }, [data, treeSteps]);
  const zips = useMemo(() => (data?.zips ?? []).filter((z) => inState(z.when, world)), [data, world]);
  const icons = useMemo(() => artIcons(art), [art]);
  const levelShow = useMemo<LevelShow>(
    () => ({
      spikes: layers.spikes,
      boxes: layers.boxes,
      blue: layers.blue,
      orange: layers.orange,
      springs: layers.springs,
    }),
    [layers],
  );
  const markerKinds = useMemo(
    () => ({
      ...Object.fromEntries(Object.keys(MARKERS).map((k) => [k, layers[`marker:${k}`]])),
      ...Object.fromEntries(Object.keys(BOX_KINDS).map((c) => [`box:${c}`, layers[`marker:box:${c}`]])),
    }),
    [layers],
  );

  const floorAt = (p: P, reach: number) => {
    let best = -1,
      near = reach;
    (data?.floors ?? []).forEach((f, i) => {
      if (!floorOn[i] || !layers[`floor:${data!.kinds[f[4]]}`]) return;
      const x = Math.min(f[2], Math.max(f[0], p[0])),
        d = Math.hypot(x - p[0], floorY(f, x) - p[1]);
      if (d < near) {
        near = d;
        best = i;
      }
    });
    return best;
  };

  const segmentAt = (p: P) => {
    if (!layers.tree || !data?.grownStep) return null;
    const i = data.grown.findIndex((s) => insidePoly(s.p, p[0], p[1]));
    return i >= 0 ? (treeSteps[data.grownStep[i]] ?? null) : null;
  };

  const onTap = (p: P, reach: number, shiftKey: boolean) => {
    if (shiftKey || shifts.placing || shifts.mode === "target") return shifts.place(p);
    const zip = layers.zips ? zipAt(zips, p, reach) : undefined;
    if (zip) {
      setPinned((s) => {
        const n = new Set(s);
        if (!n.delete(zip)) n.add(zip);
        return n;
      });
      return;
    }
    const tip = segmentAt(p);
    let box: Marker | null = null,
      best = reach;
    for (const m of markers) {
      if (m.kind !== "box" || !markerKinds[layerOf(m)]) continue;
      const d = Math.hypot(m.x - p[0], m.y - p[1]);
      if (d < best) {
        best = d;
        box = m;
      }
    }
    setBoxTip(box);
    setTreeTip(box ? null : tip);
    if (!box && !tip) {
      setFloor(floorAt(p, reach));
      setFocus(null);
    }
  };

  const onPointer = (p: P | null, hoverReach: number | null) => {
    setCursor(p);
    if (!p) {
      setHoverFloor(-1);
      setHoverMark(null);
      setHoverTree(null);
      return;
    }
    if (hoverReach === null) return;
    setHoverFloor(floorAt(p, hoverReach));
    setHoverTree(segmentAt(p));
    let mark: Marker | null = null,
      best = hoverReach;
    for (const m of markers) {
      if ((m.kind !== "box" && m.seq == null) || !markerKinds[layerOf(m)]) continue;
      const d = Math.hypot(m.x - p[0], m.y - p[1]);
      if (d < best) {
        best = d;
        mark = m;
      }
    }
    setHoverMark(mark);
  };

  const { svg, view, setView, size, handlers } = usePanZoom({ onTap, onPointer });
  const share = useShareLink(data, size, view, setView, world, setWorld);

  const px = view ? view.w / size.w : 1;
  const artShown = !!art && layers.art;
  const inView = view ? ([view.x, -(view.y + viewHeight(view, size)), view.x + view.w, -view.y] as const) : null;
  const tilesFor = (pick: StatePick, on: boolean) =>
    artShown && on && inView ? artTiles(art, pick, ...inView, px) : [];
  const f = data && floor >= 0 ? data.floors[floor] : null;
  const out = floor >= 0 && direction !== "in" ? byFloor.out[floor] : [];
  const into = floor >= 0 && direction !== "out" ? byFloor.into[floor] : [];
  const listed: Listed[] = [
    ...out.map((p) => ({ p, dir: "out" as const })),
    ...into.map((p) => ({ p, dir: "in" as const })),
  ].sort((a, b) => b.p.drop - a.p.drop);
  const hover = data && hoverFloor >= 0 && hoverFloor !== floor ? data.floors[hoverFloor] : null;
  const labels = (data?.labels ?? []).filter((l) => inState(l.when, world));
  const showLabel = (name: string) => layers[name.startsWith("bonus") ? "bonusLabels" : "labels"];
  // A hovered refill orb looks up the box that unlocks its tier; else the hovered box wins, and a clicked box
  // (its card open) keeps the highlight until cleared.
  const hoverOrb = hoverMark?.seq != null ? hoverMark : null;
  const hiBox = hoverOrb ? (markers.find((m) => orbTier(m.box) === hoverOrb.seq) ?? null) : (hoverMark ?? boxTip);
  const hiTier = hoverOrb ? hoverOrb.seq : orbTier(hiBox?.box);
  const treeHi = hoverTree ?? treeTip;
  const treeGrower = treeHi ? growers[treeSteps.indexOf(treeHi)] : null;
  const hiOrbs = useMemo(() => {
    if (hiTier == null) return { dash: 0, jump: 0, full: 0 };
    const c = { dash: 0, jump: 0, full: 0 };
    for (const m of markers)
      if (m.seq === hiTier) c[m.kind === "dashRefill" ? "dash" : m.kind === "jumpRefill" ? "jump" : "full"]++;
    return c;
  }, [markers, hiTier]);

  return (
    <div className="app">
      <header className="bar">
        <button disabled={!data} onClick={() => data && setView(fitView(data.bounds, size))}>
          Fit
        </button>
        {data?.states && <StatePicker states={data.states} pick={world} onPick={setWorld} />}
        <button
          className="end"
          disabled={!share.link}
          onClick={share.copy}
          title="Copy a link to this view (the address bar has it too)"
        >
          {share.copied ? "Copied" : "Share"}
        </button>
        <nav className="links">
          <a
            href="https://github.com/ChandlerFerry/igtap-ultra-map"
            target="_blank"
            rel="noreferrer"
            title="GitHub"
            aria-label="GitHub"
          >
            <svg viewBox="0 0 16 16" width="18" height="18" fill="currentColor" aria-hidden="true">
              <path d="M8 0c4.42 0 8 3.58 8 8a8.013 8.013 0 0 1-5.45 7.59c-.4.08-.55-.17-.55-.38 0-.27.01-1.13.01-2.2 0-.75-.25-1.23-.54-1.48 1.78-.2 3.65-.88 3.65-3.95 0-.88-.31-1.59-.82-2.15.08-.2.36-1.02-.08-2.12 0 0-.67-.22-2.2.82-.64-.18-1.32-.27-2-.27-.68 0-1.36.09-2 .27-1.53-1.03-2.2-.82-2.2-.82-.44 1.1-.16 1.92-.08 2.12-.51.56-.82 1.28-.82 2.15 0 3.06 1.86 3.75 3.64 3.95-.23.2-.44.55-.51 1.07-.46.21-1.61.55-2.33-.66-.15-.24-.6-.83-1.23-.82-.67.01-.27.38.01.53.34.19.73.9.82 1.13.16.45.68 1.31 2.69.94 0 .67.01 1.3.01 1.49 0 .21-.15.45-.55.38A7.995 7.995 0 0 1 0 8c0-4.42 3.58-8 8-8Z" />
            </svg>
          </a>
          <a href="https://igtapgame.com/" target="_blank" rel="noreferrer" title="IGTAP Wiki" aria-label="IGTAP Wiki">
            <svg viewBox="0 0 16 16" width="18" height="18" fill="currentColor" aria-hidden="true">
              <path d="M0 1.75A.75.75 0 0 1 .75 1h4.253c1.227 0 2.317.59 3 1.501A3.743 3.743 0 0 1 11.006 1h4.245a.75.75 0 0 1 .75.75v10.5a.75.75 0 0 1-.75.75h-4.507a2.25 2.25 0 0 0-1.591.659l-.622.621a.75.75 0 0 1-1.06 0l-.622-.621A2.25 2.25 0 0 0 5.258 13H.75a.75.75 0 0 1-.75-.75Zm7.251 10.324.004-5.073-.002-2.253A2.25 2.25 0 0 0 5.003 2.5H1.5v9h3.757a3.75 3.75 0 0 1 1.994.574ZM8.755 4.75l-.004 7.322a3.752 3.752 0 0 1 1.992-.572H14.5v-9h-3.495a2.25 2.25 0 0 0-2.25 2.25Z" />
            </svg>
          </a>
        </nav>
      </header>
      {ring && layers.rings && <ShiftBar s={shifts} ring={ring} />}
      {failure && <p className="failure">{failure}</p>}
      <div className="mapWrap">
        <svg
          ref={svg}
          className="map"
          viewBox={view ? `${view.x} ${view.y} ${view.w} ${viewHeight(view, size)}` : "0 0 1 1"}
          {...handlers}
        >
          <rect x={-1e6} y={-1e6} width={2e6} height={2e6} fill={BG} />
          <Art tiles={tilesFor({ ...world, bg: "on" }, layers.background)} />
          <Art tiles={tilesFor(world, true)} />
          <Art tiles={tilesFor({ ...world, tree: "grown" }, layers.tree)} />
          {(!artShown || layers.shapes) && <Level paths={level} show={levelShow} px={px} />}
          {artShown && !layers.shapes && layers.spikes && <Hazards paths={level} show={levelShow} />}
          {layers.tree && (
            <TreeSegments
              paths={treePaths}
              labels={treeLabels}
              steps={treeSteps}
              hi={treeHi}
              grower={treeGrower}
              px={px}
            />
          )}
          {data?.kinds.map(
            (k, i) =>
              layers[`floor:${k}`] && (
                <path
                  key={k}
                  d={floorPaths[i]}
                  fill="none"
                  stroke={kindColor(k)}
                  strokeWidth={2}
                  strokeOpacity={f ? 0.35 : 1}
                />
              ),
          )}
          {ring && data && layers.run && <RunShifts route={data.rebase.route} ring={ring} />}
          {ring && layers.rings && <ShiftRings s={shifts} ring={ring} px={px} />}
          {!f &&
            layers.drops &&
            dropPaths.map((p) => (
              <path key={p.color} d={p.d} fill="none" stroke={p.color} strokeWidth={6} strokeLinecap="round" />
            ))}
          <ArrowDefs px={px} />
          {f && data && <FloorPairs floor={f} floors={data.floors} out={out} into={into} focus={focus} />}
          {hover && (
            <path
              d={floorPath(hover)}
              stroke={WHITE}
              strokeOpacity={0.6}
              strokeWidth={8}
              strokeLinecap="round"
              pointerEvents="none"
            />
          )}
          {focus?.ultra && <UltraLegs legs={ultraLegs(focus.ultra)} />}
          {focus && <DropLabel pair={focus} px={px} />}
          <Labels labels={labels} px={px} show={showLabel} />
          {layers.zips && <Zips zips={zips} px={px} pinned={pinned} />}
          <Marks
            icons={icons}
            markers={markers}
            px={px}
            kinds={markerKinds}
            areas={layers.areas}
            checkpointAreas={layers.checkpointAreas}
            hiTier={hiTier}
            hiBox={hiTier != null ? hiBox : null}
          />
        </svg>
        <Legend
          kinds={data?.kinds ?? []}
          icons={icons}
          layers={layers}
          onToggle={(key) => setLayers((l) => ({ ...l, [key]: !l[key] }))}
          open={legendOpen}
          onOpen={setLegendOpen}
          which={which}
          onWhich={(w) => {
            setWhich(w);
            setFocus(null);
          }}
          direction={direction}
          onDirection={setDirection}
        />
        <div className="overlay">
          {boxTip?.box ? (
            <BoxCard
              marker={boxTip}
              orbs={hiBox === boxTip && hiTier != null ? { tier: hiTier, ...hiOrbs } : null}
              onClose={() => setBoxTip(null)}
            />
          ) : f && data ? (
            <FloorCard
              floor={f}
              floors={data.floors}
              counts={pairCounts(byFloor, floor)}
              listed={listed}
              focus={focus}
              onFocus={setFocus}
              onClose={() => {
                setFloor(-1);
                setFocus(null);
              }}
            />
          ) : (
            <Hint
              tree={treeHi}
              grower={treeGrower}
              hover={hover ? `Floor ${where(hover)} · ${pairCounts(byFloor, hoverFloor)}` : null}
            />
          )}
          {cursor && (
            <span className="readout">
              {fmt(cursor)}
              {ring &&
                shifts.last &&
                ` · ${dist(shifts.last, cursor, ring).toFixed(0)} u from ${shifts.label(shifts.chain.length - 1)} (ring ${ring.t})`}
            </span>
          )}
        </div>
      </div>
      {shifts.mode === "target" && shifts.routesOpen && shifts.target && ring && data && (
        <RouteModal
          target={shifts.target}
          radius={shifts.radius}
          routes={shifts.routes}
          pick={shifts.pick}
          ring={ring}
          center={shifts.center}
          floors={data.floors}
          kinds={data.kinds}
          level={level}
          show={levelShow}
          onPick={shifts.setPick}
          onClose={() => shifts.setRoutesOpen(false)}
          onUse={() => {
            if (!shifts.route) return;
            shifts.adopt(shifts.route.points);
            shifts.setRoutesOpen(false);
          }}
          onShow={(p) => {
            shifts.setRoutesOpen(false);
            setView(centeredView(p, 3000, size));
          }}
        />
      )}
    </div>
  );
}
