import type { StatePick, WorldState } from "./data";

const optionText = (o: string) => (o === "vman" ? "VMAN" : o[0].toUpperCase() + o.slice(1));

export function StatePicker({
  states,
  pick,
  onPick,
}: {
  states: WorldState[];
  pick: StatePick;
  onPick: (p: StatePick) => void;
}) {
  return states.map((s) => (
    <label key={s.id} title={`The level with ${s.label} in that state`}>
      <select
        aria-label={s.label}
        value={pick[s.id] ?? s.current}
        onChange={(e) => onPick({ ...pick, [s.id]: e.target.value })}
      >
        {s.options.map((o) => (
          <option key={o} value={o}>
            {optionText(o)}
          </option>
        ))}
      </select>
    </label>
  ));
}
