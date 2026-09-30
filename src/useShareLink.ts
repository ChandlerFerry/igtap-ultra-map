import { useEffect, useState } from "react";
import type { StatePick, UltraData } from "./data";
import { fitView, readHash, viewHash, type Size, type View } from "./view";

export function useShareLink(
  data: UltraData | null,
  size: Size,
  view: View | null,
  setView: (v: View) => void,
  world: StatePick,
  setWorld: (w: StatePick) => void,
) {
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!data || view || size.w <= 1) return;
    const shared = readHash(location.hash, data.states, size);
    if (shared) setWorld(shared.world);
    setView(shared?.view ?? fitView(data.bounds, size));
  }, [data, view, size]);

  useEffect(() => {
    if (!data) return;
    const onHash = () => {
      const shared = readHash(location.hash, data.states, size);
      if (!shared) return;
      setWorld(shared.world);
      setView(shared.view);
    };
    addEventListener("hashchange", onHash);
    return () => removeEventListener("hashchange", onHash);
  }, [data, size]);

  const link = view && data ? `#${viewHash(view, size, data.states, world)}` : "";

  useEffect(() => {
    if (!link) return;
    const t = setTimeout(() => history.replaceState(null, "", link), 300);
    return () => clearTimeout(t);
  }, [link]);

  const copy = () =>
    navigator.clipboard.writeText(location.href.split("#")[0] + link).then(
      () => {
        setCopied(true);
        setTimeout(() => setCopied(false), 1500);
      },
      () => {},
    );

  return { link, copied, copy };
}
