import type { Marker, TreeStep } from "./data";
import { where } from "./geometry";
import { boxCost, boxUses, freshText, secretNumber, treeStepText } from "./Marks";
import { pairColor, type Pair, type Ultra } from "./pairs";
import { BOX_KINDS, IN_COLOR } from "./theme";

const LIST = 24;

export type Listed = { p: Pair; dir: "out" | "in" };

function Close({ label, onClick }: { label: string; onClick: () => void }) {
  return (
    <button className="close" onClick={onClick} aria-label={label}>
      ×
    </button>
  );
}

export function BoxCard({ marker, onClose }: { marker: Marker; onClose: () => void }) {
  const b = marker.box!;
  return (
    <div className="panel">
      <div>
        <strong>{marker.name}</strong> · {BOX_KINDS[b.category].text}
        <Close label="Clear the picked box" onClick={onClose} />
      </div>
      <div className="muted">
        Gives {b.gives} ({b.upgrade}) · {boxCost(b)} · {boxUses(b)}
      </div>
      <div className="muted">
        {b.secret ? `Secret${secretNumber(b)} · ` : ""}
        {b.ach ? `${b.ach} · ` : ""}
        {b.tree ? "tree upgrade · " : ""}
        {freshText(b)}
        {marker.when ? ` · only in ${marker.when}` : ""}
      </div>
    </div>
  );
}

function UltraText({ u }: { u: Ultra }) {
  return (
    <div className="muted">
      Engine ultra {u.x.toFixed(0)}, {u.y.toFixed(0)} ·{" "}
      {u.shift ? `origin shift (drop ${u.drop.toFixed(1)})` : `drop ${u.drop.toFixed(1)}`} · hyper {u.hyper} · launch{" "}
      {u.fromX.toFixed(0)}, {u.fromY.toFixed(0)} {u.side > 0 ? "→" : "←"} · dash {u.delay}, jump {u.jump}, hold {u.hold}
      {u.gap > 0 && `, 2nd dash +${u.gap}, jump ${u.jump2}`}
    </div>
  );
}

export function FloorCard({
  floor,
  floors,
  counts,
  listed,
  focus,
  onFocus,
  onClose,
}: {
  floor: number[];
  floors: number[][];
  counts: string;
  listed: Listed[];
  focus: Pair | null;
  onFocus: (p: Pair | null) => void;
  onClose: () => void;
}) {
  return (
    <div className="panel">
      <div>
        <strong>Floor {where(floor)}</strong> · {counts}
        {listed.length > LIST && ` · top ${LIST}`}
        <Close label="Clear the selected floor" onClick={onClose} />
      </div>
      <div className="pairs">
        {listed.slice(0, LIST).map(({ p, dir }) => (
          <button
            key={`${dir}${p.index}`}
            className={focus === p ? "pair on" : "pair"}
            style={{ borderColor: dir === "out" ? pairColor(p) : IN_COLOR }}
            title={p.line ? "A flight line clears the level" : "Only the engine found this one"}
            onClick={() => onFocus(focus === p ? null : p)}
          >
            {dir === "out" ? "to" : "from"} {where(floors[dir === "out" ? p.to : p.from])} · ↓{p.drop.toFixed(0)}
            {p.ultra ? " ✓" : ""}
            {p.from === p.to ? " · downhill" : ""}
          </button>
        ))}
        {listed.length === 0 && <span className="muted">No ultra pairs here.</span>}
      </div>
      {focus?.ultra && <UltraText u={focus.ultra} />}
    </div>
  );
}

export function Hint({ tree, hover }: { tree: TreeStep | null; hover: string | null }) {
  return (
    <span className="muted">
      {tree
        ? treeStepText(tree)
        : (hover ??
          "Tap a buy box for what it is, gives and costs, a floor for where its ultras go and come from, or a tree number for what grows it.")}
    </span>
  );
}
