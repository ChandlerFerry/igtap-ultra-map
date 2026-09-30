import { fmt } from "./geometry";
import { angleOf, type Ring } from "./shifts";
import type { Mode, Shifts } from "./useShifts";

const MODES: [Mode, string][] = [
  ["forward", "Forward"],
  ["backward", "Backward"],
  ["target", "Target"],
];

function ForwardTools({ s }: { s: Shifts }) {
  return (
    <>
      <button disabled={!s.last} onClick={s.extend}>
        Next shift
      </button>
      <button disabled={s.fwd.length <= 1} onClick={() => s.setFwd((c) => c.slice(0, -1))}>
        Undo
      </button>
      <button disabled={!s.runShifts.length} onClick={() => s.setFwd(s.runShifts)}>
        Run&apos;s shifts
      </button>
    </>
  );
}

function TargetTools({ s }: { s: Shifts }) {
  return (
    <>
      <label title="How far from the tapped point the last shift may land">
        Target radius{" "}
        <input
          type="number"
          min={1}
          step={16}
          value={s.radius}
          onChange={(e) => s.setRadius(Math.max(1, Number(e.target.value) || 1))}
        />{" "}
        u
      </label>
      {s.target && <button onClick={() => s.setRoutesOpen(true)}>Routes ({s.routes.length})</button>}
      {s.route && <button onClick={() => s.adopt(s.route!.points)}>Use as forward</button>}
      <span className="muted">{s.target ? `to ${fmt(s.target)}` : "Tap the map to drop the target"}</span>
    </>
  );
}

function BackwardTools({ s }: { s: Shifts }) {
  return (
    <>
      <button disabled={!s.last} onClick={s.extend}>
        Previous shift
      </button>
      <button disabled={!s.back.length} onClick={() => s.setBack((c) => c.slice(0, -1))}>
        Undo
      </button>
      {s.closes.map((p, i) => (
        <button key={i} onClick={() => s.setBack((c) => [...c, p])}>
          Close at {fmt(p)}
        </button>
      ))}
      {s.closed && s.back.length > 0 && <button onClick={() => s.adopt([...s.back].reverse())}>Use as forward</button>}
      {s.gap !== null && (
        <span className={s.closed ? "ok" : "muted"}>
          {s.closed ? `reaches the first line: ${s.back.length} shifts` : `${s.gap.toFixed(0)} u off the first line`}
        </span>
      )}
      {!s.back.length && <span className="muted">Place or Shift+click a point</span>}
    </>
  );
}

export function ShiftBar({ s, ring }: { s: Shifts; ring: Ring }) {
  const angle = s.anchor && s.last && !ring.use2D ? angleOf(s.anchor, s.last) : null;
  return (
    <header className="bar">
      <span className="seg">
        {MODES.map(([mode, text]) => (
          <button key={mode} className={s.mode === mode ? "on" : ""} onClick={() => s.setMode(mode)}>
            {text}
          </button>
        ))}
      </span>
      {s.mode !== "target" && (
        <button
          className={s.placing ? "on" : ""}
          onClick={() => s.setPlacing((p) => !p)}
          title="Tap/click the map to place the last shift (same as Shift+click)"
        >
          Place
        </button>
      )}
      {s.mode === "forward" ? (
        <ForwardTools s={s} />
      ) : s.mode === "target" ? (
        <TargetTools s={s} />
      ) : (
        <BackwardTools s={s} />
      )}
      {angle !== null && (
        <label className="grow">
          <input
            type="range"
            min={0}
            max={360}
            step={0.001}
            value={angle}
            onChange={(e) => s.setAngle(Number(e.target.value))}
          />
          <input
            type="number"
            min={0}
            max={360}
            step={0.01}
            value={Number(angle.toFixed(3))}
            onChange={(e) => s.setAngle(Number(e.target.value))}
          />
          °
        </label>
      )}
      <span className="chips">
        {s.ordered.map((p, i) => {
          const at = s.mode === "backward" ? s.chain.length - 1 - i : i;
          return (
            <span key={i} className={at === s.chain.length - 1 ? "chip on" : "chip"}>
              {s.label(at)}: {fmt(p)}
            </span>
          );
        })}
      </span>
    </header>
  );
}
