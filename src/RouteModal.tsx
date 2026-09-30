import { useEffect, useMemo, useRef, useState } from "react";
import { dotPath, floorPath, fmt, fy, type P } from "./geometry";
import { Level, type LevelPaths, type LevelShow } from "./Level";
import type { Route } from "./routes";
import { ringPath, topArcPath, type Ring } from "./shifts";
import { BG, kindColor, RING_COLOR, SHIFT_COLOR, WHITE } from "./theme";

const SPAN = 1800;
const THUMB = { w: 300, h: 180 };
const PX = SPAN / THUMB.w;
const HEIGHT = (SPAN * THUMB.h) / THUMB.w;

const clamp = (n: number, top: number) => Math.min(top - 1, Math.max(0, n));
const sideText = (right: boolean) => (right ? "right →" : "← left");

type Props = {
  target: P;
  radius: number;
  routes: Route[];
  pick: number;
  ring: Ring;
  center: P;
  floors: number[][];
  kinds: string[];
  level: LevelPaths;
  show: LevelShow;
  onPick: (i: number) => void;
  onShow: (p: P) => void;
  onUse: () => void;
  onClose: () => void;
};

function Step({
  route,
  i,
  on,
  props,
  floorLayers,
  onSelect,
}: {
  route: Route;
  i: number;
  on: boolean;
  props: Props;
  floorLayers: { k: string; d: string }[];
  onSelect: () => void;
}) {
  const { target, radius, ring, center, floors, level, show, onShow } = props;
  const p = route.points[i],
    from = i ? route.points[i - 1] : center,
    last = i === route.points.length - 1,
    f = last ? null : floors[route.floors[i]];
  const outward = f ? (p[0] - from[0]) * (f[2] - f[0]) + (p[1] - from[1]) * (f[3] - f[1]) >= 0 : route.right;
  const x = p[0] - SPAN / 2,
    y = fy(p[1]) - HEIGHT / 2;
  return (
    <div className={on ? "step on" : "step"} onClick={onSelect}>
      <svg width={THUMB.w} height={THUMB.h} viewBox={`${x} ${y} ${SPAN} ${HEIGHT}`}>
        <rect x={x} y={y} width={SPAN} height={HEIGHT} fill={BG} />
        <Level paths={level} show={show} px={PX} />
        {floorLayers.map(({ k, d }) => (
          <path key={k} d={d} fill="none" stroke={kindColor(k)} strokeWidth={2} />
        ))}
        <path d={ringPath(from, ring)} fill="none" stroke={RING_COLOR} strokeWidth={1.5} strokeDasharray="8 6" />
        {last && !ring.use2D && <path d={topArcPath(from, ring)} fill="none" stroke={SHIFT_COLOR} strokeWidth={2.5} />}
        {last && (
          <circle
            cx={target[0]}
            cy={fy(target[1])}
            r={radius}
            fill={WHITE}
            fillOpacity={0.12}
            stroke={WHITE}
            strokeWidth={1.5}
          />
        )}
        {f && <path d={floorPath(f)} stroke={WHITE} strokeWidth={5} strokeLinecap="round" />}
        <path
          d={`M${p[0]} ${fy(p[1]) - 60}h${outward ? 160 : -160}`}
          stroke={SHIFT_COLOR}
          strokeWidth={2}
          markerEnd={`url(#step-arrow-${i})`}
        />
        <path d={dotPath(p[0], p[1])} stroke={last ? WHITE : RING_COLOR} strokeWidth={10} strokeLinecap="round" />
        <defs>
          <marker
            id={`step-arrow-${i}`}
            viewBox="0 0 10 10"
            refX={9}
            refY={5}
            markerUnits="userSpaceOnUse"
            markerWidth={60}
            markerHeight={60}
            orient="auto"
          >
            <path d="M0 0L10 5L0 10Z" fill={SHIFT_COLOR} />
          </marker>
        </defs>
      </svg>
      <div className="stepText">
        <strong>
          Shift {i + 1}
          {last && ` · ultra ${sideText(outward)}`}
        </strong>
        <button onClick={() => onShow(p)}>Show on map</button>
      </div>
    </div>
  );
}

export function RouteModal(props: Props) {
  const { target, routes, pick, floors, kinds, onPick, onUse, onClose } = props;
  const route = routes[pick] ?? null;
  const count = route?.points.length ?? 0;
  const [step, setStep] = useState(0);
  const box = useRef<HTMLDivElement>(null);
  const floorLayers = useMemo(
    () =>
      kinds.map((k, i) => ({
        k,
        d: floors
          .filter((f) => f[4] === i)
          .map(floorPath)
          .join(""),
      })),
    [floors, kinds],
  );

  useEffect(() => setStep(0), [pick]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const dir = e.key === "ArrowDown" || e.key === "ArrowRight" ? 1 : -1;
      if (e.key === "Escape") onClose();
      else if ((e.key === "ArrowUp" || e.key === "ArrowDown") && routes.length)
        onPick(clamp(pick + dir, routes.length));
      else if ((e.key === "ArrowLeft" || e.key === "ArrowRight") && count) setStep((s) => clamp(s + dir, count));
      else return;
      e.preventDefault();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose, onPick, pick, routes.length, count]);

  useEffect(() => {
    for (const selector of [".routeList .on", ".step.on"])
      box.current?.querySelector(selector)?.scrollIntoView({ block: "nearest", inline: "nearest" });
  }, [pick, step]);

  return (
    <div
      className="modalBack"
      onPointerDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div ref={box} className="modal" role="dialog" aria-modal="true" aria-label="Shift routes">
        <div className="modalHead">
          <strong>Routes to {fmt(target)}</strong>
          <span className="grow" />
          {route && <button onClick={onUse}>Use as forward</button>}
          <button className="close" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>
        {routes.length === 0 ? (
          <p className="muted">
            No route within 20 shifts lands here on a top hemisphere; move the target or widen it.
          </p>
        ) : (
          <div className="modalBody">
            <div className="routeList">
              {routes.map((r, i) => (
                <button
                  key={i}
                  className={i === pick ? "pair on" : "pair"}
                  style={{ borderColor: SHIFT_COLOR }}
                  onClick={() => onPick(i)}
                >
                  {r.points.length} shift{r.points.length === 1 ? "" : "s"} · {sideText(r.right)}
                </button>
              ))}
            </div>
            {route && (
              <div className="steps">
                {route.points.map((_, i) => (
                  <Step
                    key={i}
                    route={route}
                    i={i}
                    on={i === step}
                    props={props}
                    floorLayers={floorLayers}
                    onSelect={() => setStep(i)}
                  />
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
