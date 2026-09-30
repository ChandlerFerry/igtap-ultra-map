import type { ReactNode } from "react";
import type { MarkIcon } from "./Art";
import type { Direction, Which } from "./settings";
import {
  BANDS,
  BOX_COLOR,
  BOX_KINDS,
  GROWN_COLOR,
  IN_COLOR,
  KIND_TEXT,
  kindColor,
  LEVEL_COLOR,
  MARKERS,
  RING_COLOR,
  SHIFT_COLOR,
  SPIKE_COLOR,
  SPRING_COLOR,
  TONE_COLORS,
  ZIP_COLOR,
  type Look,
} from "./theme";

type Row = { layer?: string; text: string; swatch: ReactNode };

const line = (color: string) => <i style={{ background: color }} />;
const dot = (color: string) => <i className="dot" style={{ background: color }} />;
const glyph = (text: string, color?: string) => (
  <i className="blank" style={color ? { color } : undefined}>
    {text}
  </i>
);

function shape(look: Look, icon?: MarkIcon) {
  if (icon) return <img className="icon" src={icon.href} alt="" />;
  if (look.shape === "circle") return dot(look.color);
  if (look.shape === "star") return glyph("★", look.color);
  return <i className="square" style={{ background: look.color }} />;
}

const bandText = (i: number) => {
  const top = BANDS[i][0];
  return `${i === 0 ? "4" : BANDS[i - 1][0]}${top === Infinity ? "+" : `–${top}`} u`;
};

function sections(kinds: string[], icons: Record<string, MarkIcon>): [string, Row[]][] {
  return [
    [
      "Floors",
      [
        ...kinds.map((k) => ({ layer: `floor:${k}`, text: KIND_TEXT[k] ?? k, swatch: line(kindColor(k)) })),
        {
          layer: "drops",
          text: "Biggest drop onto floor",
          swatch: (
            <i
              className="bands"
              style={{ background: `linear-gradient(90deg,${BANDS.map(([, c]) => c).join(",")})` }}
            />
          ),
        },
        ...BANDS.map(([, color], i) => ({ text: bandText(i), swatch: dot(color) })),
        { text: "Origin shift", swatch: dot(SHIFT_COLOR) },
        { text: "Onto selected floor", swatch: line(IN_COLOR) },
      ],
    ],
    [
      "Level",
      [
        { layer: "background", text: "Background art", swatch: glyph("▦") },
        { layer: "art", text: "Game art", swatch: glyph("▦") },
        { layer: "tree", text: "Tree", swatch: glyph("▲", GROWN_COLOR) },
        { layer: "shapes", text: "Collision shapes", swatch: line(LEVEL_COLOR) },
        { layer: "spikes", text: "Hazards", swatch: line(SPIKE_COLOR) },
        { layer: "blue", text: "Blue blocks/spikes", swatch: line(TONE_COLORS.blue) },
        { layer: "orange", text: "Orange blocks/spikes", swatch: line(TONE_COLORS.orange) },
        { layer: "springs", text: "Springs", swatch: line(SPRING_COLOR) },
        { layer: "boxes", text: "Upgrade boxes", swatch: line(BOX_COLOR) },
        { layer: "zips", text: "Zips", swatch: line(ZIP_COLOR) },
        { layer: "grown", text: "Tree ground outline", swatch: line(GROWN_COLOR) },
      ],
    ],
    [
      "Markers",
      [
        ...Object.entries(MARKERS).map(([k, look]) => ({
          layer: `marker:${k}`,
          text: look.text,
          swatch: shape(look, icons[k]),
        })),
        {
          layer: "checkpointAreas",
          text: "Checkpoint trigger area",
          swatch: <i className="area" style={{ borderColor: MARKERS.checkpoint.color }} />,
        },
      ],
    ],
    [
      "Buy boxes",
      Object.entries(BOX_KINDS).map(([k, look]) => ({
        layer: `marker:box:${k}`,
        text: look.text,
        swatch: shape(look),
      })),
    ],
    [
      "Shifts",
      [
        { layer: "rings", text: "Shift rings and chain", swatch: line(RING_COLOR) },
        { layer: "run", text: "Run's shifts", swatch: dot(SHIFT_COLOR) },
        { layer: "labels", text: "Course labels", swatch: glyph("A") },
        { layer: "bonusLabels", text: "Bonus labels", swatch: glyph("A") },
      ],
    ],
  ];
}

export function Legend({
  kinds,
  icons,
  layers,
  onToggle,
  open,
  onOpen,
  which,
  onWhich,
  direction,
  onDirection,
}: {
  kinds: string[];
  icons: Record<string, MarkIcon>;
  layers: Record<string, boolean>;
  onToggle: (layer: string) => void;
  open: boolean;
  onOpen: (open: boolean) => void;
  which: Which;
  onWhich: (w: Which) => void;
  direction: Direction;
  onDirection: (d: Direction) => void;
}) {
  return (
    <aside className={open ? "legend" : "legend closed"}>
      <button className="legendHead" onClick={() => onOpen(!open)} aria-expanded={open}>
        {open ? "▾" : "▸"} Legend
      </button>
      {open &&
        sections(kinds, icons).map(([title, rows]) => (
          <section key={title}>
            <h3>{title}</h3>
            {title === "Floors" && (
              <>
                <label className="row key">
                  Drops{" "}
                  <select value={which} onChange={(e) => onWhich(e.target.value as Which)}>
                    <option value="all">Every drop</option>
                    <option value="engine">Engine-confirmed</option>
                    <option value="shift">Origin-shift</option>
                  </select>
                </label>
                <label className="row key">
                  Selected floor{" "}
                  <select value={direction} onChange={(e) => onDirection(e.target.value as Direction)}>
                    <option value="both">From and onto</option>
                    <option value="out">From</option>
                    <option value="in">Onto</option>
                  </select>
                </label>
              </>
            )}
            {rows.map(({ layer, text, swatch }) =>
              layer ? (
                <label key={text} className="row">
                  <input type="checkbox" checked={!!layers[layer]} onChange={() => onToggle(layer)} />
                  {swatch}
                  {text}
                </label>
              ) : (
                <div key={text} className="row key">
                  {swatch}
                  {text}
                </div>
              ),
            )}
          </section>
        ))}
    </aside>
  );
}
